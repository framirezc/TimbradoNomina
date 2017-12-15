using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using CommonFunctions.Functions;
using CommonFunctions.DataAccess;
using System.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Xml;
using TimbradoNomina.NominaObjects;


namespace TimbradoNomina
{
    class AppBussinessLogic
    {

        string data;

        public string Data
        {
            get { return data; }
            set { data = value; }
        }

        string _LogPath = string.Empty;
        int _PDFError = 0;
        int _Error = 0;
        int _Timbrado = 0;
        int _NoTimbrado = 0;
        int _Iteraciones = 0;

        public void StartProcessDirectory()
        {
            if (!JsonIsvalid(data)) return;
            try { StartStamp(data); }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void SetConfigurationFromFile(Configuracion conf)
        {
            conf.XsltPath = ConfigurationManager.AppSettings["xsltPath"];
            conf.XsdPath = ConfigurationManager.AppSettings["xsdPath"];
            conf.NameSpace = ConfigurationManager.AppSettings["xsdNameSpace"];
            conf.QrPath = ConfigurationManager.AppSettings["qrSavePath"];
            conf.ReportUrl = ConfigurationManager.AppSettings["reportUrl"];
            conf.RequiredPath = ConfigurationManager.AppSettings["requiredPath"];
            conf.OpenSSLStartPath = ConfigurationManager.AppSettings["opensslAppPath"];
            conf.QrUrl = ConfigurationManager.AppSettings["qrUrl"];
            conf.CFDINameSpace = ConfigurationManager.AppSettings["cfdiNameSpace"];
            WriteLog("Parametros asignados webconfig");
        }

        private void SetConfigurationDataBase(Configuracion conf, Carpeta folder)
        {
            SqlParameter[] parameterList = { new SqlParameter("@idEmpresa", folder.IDEmpresa) };

            SqlServer BaseNomina = new SqlServer();
            BaseNomina.ConnectionString = ConfigurationManager.ConnectionStrings["cnxBaseNomina"].ToString();
            System.Data.DataSet ds = BaseNomina.ExecuteQueryProcedure("SEL_CERTIFICADOS_SP", parameterList);

            if (ds.Tables[0].Rows.Count > 0)
            {
                conf.KeyPemPath = conf.RequiredPath + ds.Tables[0].Rows[0][0].ToString();
                conf.SuscriptorRFC = ds.Tables[0].Rows[0][1].ToString();
                conf.AgenteTI = ds.Tables[0].Rows[0][2].ToString();
                conf.CertB64Content = ds.Tables[0].Rows[0][3].ToString();
                conf.CertificateNumber = ds.Tables[0].Rows[0][4].ToString();
            }

            WriteLog("Parametros asignados Base de Datos");
        }



        private void ProcessFile(Carpeta carpetaTimbrar, Configuracion configuraTimbrado, ref string[] files, int current)
        {
            if (current >= files.Length)
            {
                string resultado = string.Format("Carpeta:{0}|TotalDocs:{1}|Timbrados:{2}|NoTimbrados:{3}|Error:{4}",
                       carpetaTimbrar.NombreDirectorio, carpetaTimbrar.NoDocumentos, _Timbrado, _NoTimbrado, _Error);
                
                Console.WriteLine(resultado);
                Console.WriteLine("PDFs no generados:" + _PDFError.ToString());
                WriteLog(resultado, "FNP");

                if (!carpetaTimbrar.HuboError)
                    UpdateProcessedDirectory(carpetaTimbrar);

                    DeleteQrFiles(configuraTimbrado);

                return;
            }

            string metodo = string.Empty;

            Documento documento = new Documento();
            documento.NombreXml = Path.GetFileNameWithoutExtension(files[current]);
            documento.RutaDirectorio = carpetaTimbrar.RutaDirectorio;
            documento.RutaDirectorioError = carpetaTimbrar.BadPath;
            documento.RutaDirectorioOk = carpetaTimbrar.OkPath;
            documento.IDNomina = carpetaTimbrar.IDNomina;
            documento.RutaXml = files[current];
            documento.CodigoResultado = "-1";
            documento.XmlResultString = "";
            documento.Description = "NA";
            documento.IDEmpresa = carpetaTimbrar.IDEmpresa;

            int procNumber = _Timbrado + _Error + _NoTimbrado + 1;

            Console.Write(string.Format("{0} DE {1}|{2}|{3}|", procNumber.ToString().PadLeft(4, ' '),
                carpetaTimbrar.NoDocumentos.ToString(), DateTime.Now.ToString(), documento.NombreXml));

            WriteLog("Comienza Archivo:" + documento.NombreXml, "INI");

            try
            {
                metodo = "RemoveNode"; RemoveNode(documento, configuraTimbrado);
                metodo = "CreateLayoutString"; CreateLayoutString(documento, configuraTimbrado);
                metodo = "SignLayoutString"; SignLayoutString(documento, configuraTimbrado);
                metodo = "StampFile"; StampFile(documento, configuraTimbrado);
                metodo = "LoadDocument"; documento.XmlOrigenString = LoadDocument(documento.RutaXml);
                metodo = "EncodeToBase64"; documento.XmlOrigenBase64 = EncodeToBase64(documento.XmlOrigenString);
                metodo = "Timbrar"; Timbrar(documento, configuraTimbrado);
                //metodo = "SimularTimbrar"; SimularTimbrar(documento, configuraTimbrado);
                metodo = "DeleteStampFile"; DeleteStampFile(documento);

                if (documento.CodigoResultado == "100")
                {
                    metodo = "SaveStampFile"; SaveStampFile(documento, configuraTimbrado, carpetaTimbrar);
                    metodo = "SetNewXMLParams"; SetNewXMLParams(documento, configuraTimbrado);
                    Console.Write("Timbrado");
                    _Timbrado++;

                    SetPDFValues(documento, configuraTimbrado, carpetaTimbrar);
                }
                else
                {
                    metodo = "SetEmptyInsertParams"; SetEmptyInsertParams(documento);
                    metodo = "SaveUnstampFile"; SaveUnstampFile(documento, carpetaTimbrar);
                    Console.WriteLine("NoTimbrado");
                    _NoTimbrado++;
                }

            }
            catch (PDFException pdfex)
            {
                _PDFError++;
                WriteLog(string.Format("DOC:{0}|MET:{1}|DES:{2}", documento.NombreXml, "SetPDFValues", pdfex.Message), "PDF");
            }
            catch(Exception ex)
            {
                _Error++;
                Console.WriteLine("ERROR");
                carpetaTimbrar.HuboError = true;
                SetEmptyInsertParams(documento);
                WriteLog(string.Format("DOC:{0}|MET:{1}|DES:{2}", documento.NombreXml, metodo, ex.Message), "ERR");
            }
            finally
            {
                WriteLog(string.Format("DOC:{0}|COD:{1}|DESC:{2}", documento.NombreXml, documento.CodigoResultado, documento.Description), "TIM");
                WriteLog("DOC: " + documento.NombreXml, "FIN");

                if (_Iteraciones >= 50)
                {
                    System.Threading.Thread.Sleep(3000);
                    _Iteraciones = 0;
                }

                _Iteraciones++;

                InsertProcessedFile(carpetaTimbrar, documento);
                ProcessFile(carpetaTimbrar, configuraTimbrado, ref  files, ++current);
            }

        }

        private void SetPDFValues(Documento documento, Configuracion configuraTimbrado, Carpeta carpetaTimbrar)
        {
            try
            {
                string json = GetJson(documento, configuraTimbrado, carpetaTimbrar);
                CreatePDF(json, documento, configuraTimbrado);
                Console.WriteLine("|PDFOK");
            }
            catch (Exception ex)
            {
                Console.WriteLine("|PDFNO");
                throw new PDFException(ex.Message);
            }
        }

        private string GetJson(Documento documento, Configuracion configuraTimbrado, Carpeta carpetaTimbrar)
        {
            return new Template().leerXML(documento.RutaArchivoTimbrado, configuraTimbrado.QrPath, documento.NombreXml + ".png", configuraTimbrado.QrUrl);
        }

        private void SaveStampFile(Documento documento, Configuracion configuraTimbrado, Carpeta carpetaTimbrar)
        {
            if (File.Exists(documento.RutaArchivoOk))
            {
                string tempName = "_" + DateTime.Now.ToString("yyyyMMddhhmmss") + ".tmp";
                File.Move(documento.RutaArchivoOk, documento.RutaArchivoRepetido + tempName);
            }
            File.WriteAllText(documento.RutaArchivoTimbrado, documento.XmlResultString);
            File.Move(documento.RutaXml, documento.RutaArchivoOk);

            WriteLog(string.Format("Ruta Archivo timbrado:{0}", documento.RutaArchivoTimbrado));
        }

        private void SaveUnstampFile(Documento documento, Carpeta carpetaTimbrar)
        {

            if (File.Exists(documento.RutaArchivoError))
            {
                string tempName = "_" + DateTime.Now.ToString("yyyyMMddhhmmss") + ".tmp";
                File.Move(documento.RutaArchivoError, documento.RutaArchivoRepetidoError + tempName);
            }

            File.Move(documento.RutaXml, documento.RutaArchivoError);
            WriteLog(string.Format("Archivo: {0} |Result:{1} |Desc:{2}", documento.NombreXml, documento.CodigoResultado, documento.Description));

        }

        private void SetNewXMLParams(Documento doc, Configuracion conf)
        {
            //xmlDoc.Load(doc.RutaArchivoTimbrado);
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(doc.RutaArchivoOk);

            XmlNamespaceManager nsMgr = new XmlNamespaceManager(xmlDoc.NameTable);
            nsMgr.AddNamespace("cfdi", conf.CFDINameSpace);
            nsMgr.AddNamespace("nomina12", "http://www.sat.gob.mx/nomina12");

            XmlNode nodoComprobante = xmlDoc.SelectSingleNode("/cfdi:Comprobante", nsMgr);
            XmlNode nodoReceptor = xmlDoc.SelectSingleNode("/cfdi:Comprobante/cfdi:Receptor", nsMgr);
            XmlNode nodoConcepto = xmlDoc.SelectSingleNode("/cfdi:Comprobante/cfdi:Conceptos/cfdi:Concepto", nsMgr);
            XmlNode nodoNomina = xmlDoc.SelectSingleNode("/cfdi:Comprobante/cfdi:Complemento", nsMgr).FirstChild;
            XmlNode nodoNominaReceptor = xmlDoc.SelectSingleNode("/cfdi:Comprobante/cfdi:Complemento/nomina12:Nomina/nomina12:Receptor", nsMgr);

            InsertParams param = new InsertParams();

            param.Fecha = (nodoComprobante.Attributes["fecha"] != null) ? DateTime.Parse(nodoComprobante.Attributes["fecha"].Value) : (DateTime?)null;
            param.Total = (nodoComprobante.Attributes["total"] != null) ? decimal.Parse(nodoComprobante.Attributes["total"].Value) : 0;
            param.Nombre = (nodoReceptor.Attributes["nombre"] != null) ? nodoReceptor.Attributes["nombre"].Value : string.Empty;
            param.Descripcion = (nodoConcepto.Attributes["descripcion"] != null) ? nodoConcepto.Attributes["descripcion"].Value : string.Empty;
            param.FechaPago = (nodoNomina.Attributes["FechaPago"] != null) ? DateTime.Parse(nodoNomina.Attributes["FechaPago"].Value) : (DateTime?)null;
            param.Departamento = (nodoNominaReceptor.Attributes["Departamento"] != null) ? nodoNominaReceptor.Attributes["Departamento"].Value : string.Empty;
            param.NumEmpleado = (nodoNominaReceptor.Attributes["NumEmpleado"] != null) ? nodoNominaReceptor.Attributes["NumEmpleado"].Value : string.Empty;
            param.Puesto = (nodoNominaReceptor.Attributes["Puesto"] != null) ? nodoNominaReceptor.Attributes["Puesto"].Value : string.Empty;

            doc.Parametros = param;
        }

        private void RemoveNode(Documento docOrigen, Configuracion conf)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.PreserveWhitespace = false;
            xmlDoc.Load(docOrigen.RutaXml);

            XmlNamespaceManager nsMgr = new XmlNamespaceManager(xmlDoc.NameTable);
            nsMgr.AddNamespace("cfdi", conf.CFDINameSpace);

            XmlNode nodoComprobante = xmlDoc.SelectSingleNode("/cfdi:Comprobante", nsMgr);
            XmlNode nodoImpuestos = xmlDoc.SelectSingleNode("/cfdi:Comprobante/cfdi:Impuestos", nsMgr);
            XmlNode nodoConceptos = xmlDoc.SelectSingleNode("/cfdi:Comprobante/cfdi:Conceptos", nsMgr);

            if (nodoImpuestos != null) nodoComprobante.RemoveChild(nodoImpuestos);

            //XmlElement elemImpuestos = xmlDoc.CreateElement("cfdi", "Impuestos", conf.CFDINameSpace);
            //nodoComprobante.InsertAfter(elemImpuestos, nodoConceptos);
            xmlDoc.Save(docOrigen.RutaXml);

            WriteLog("Nodo Impuesto Renombrado:" + docOrigen.NombreXml);
        }

        private void StampFile(Documento docOrigen, Configuracion conf)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(docOrigen.RutaXml);
            XmlElement root = xmlDoc.DocumentElement;

            string stamp = LoadDocument(docOrigen.RutaArchivoCadenaOriginalB64);

            root.SetAttribute("Sello", stamp);
            root.SetAttribute("Certificado", conf.CertB64Content);
            root.SetAttribute("NoCertificado", conf.CertificateNumber);

            xmlDoc.Save(docOrigen.RutaXml);

            WriteLog("Archivo sellado:" + docOrigen.NombreXml);
        }

        private void CreateLayoutString(Documento docOrigen, Configuracion conf)
        {
            string cadenaOriginal = TransformXml(docOrigen.RutaXml, conf.XsltPath);
            File.WriteAllText(docOrigen.RutaArchivoCadenaOriginal, cadenaOriginal);
        }

        private void SignLayoutString(Documento docOrigen, Configuracion conf)
        {
            string argsString = string.Format("dgst -sha256 -out \"{0}\" -sign \"{1}\" \"{2}\" ", docOrigen.RutaArchivoFirma, conf.KeyPemPath, docOrigen.RutaArchivoCadenaOriginal);
            ExcuteCommandLine(conf.OpenSSLStartPath, argsString);
            argsString = string.Format("enc -in \"{0}\" -a -A -out \"{1}\" ", docOrigen.RutaArchivoFirma, docOrigen.RutaArchivoCadenaOriginalB64);
            ExcuteCommandLine(conf.OpenSSLStartPath, argsString);
            WriteLog("Archivo Firmado:" + docOrigen.NombreXml);
            System.Threading.Thread.Sleep(500);
        }

        private void InsertProcessedFile(Carpeta carpeta, Documento doc)
        {
            SqlParameter[] parameterList = {
                new SqlParameter("@idEmpresa",doc.IDEmpresa),
                new SqlParameter("@idTipo",carpeta.IDTipo),
                new SqlParameter("@idUsuario",carpeta.IDUsuario ),
                new SqlParameter("@NombreArchivo",doc.NombreXml),
                new SqlParameter("@estatus", doc.CodigoResultado),
                new SqlParameter("@idNomina",doc.IDNomina),
                new SqlParameter("@descripcionEstatus",doc.Description),
                new SqlParameter("@totalPago",doc.Parametros.Total),
                new SqlParameter("@nombreEmpleado",doc.Parametros.Nombre),
                new SqlParameter("@IdEmpleado",doc.Parametros.NumEmpleado),
                new SqlParameter("@Departamentoxml",doc.Parametros.Departamento),
                new SqlParameter("@Puestoxml",doc.Parametros.Puesto )
                };

            SqlServer BaseNomina = new SqlServer();
            BaseNomina.ConnectionString = ConfigurationManager.ConnectionStrings["cnxBaseNomina"].ToString();
            BaseNomina.ExecuteNonQueryProcedure("INS_TIMBRADO_SP", parameterList);

        }

        private void ConvertJsonToClass(string json, Carpeta carpeta)
        {
            dynamic obj = JsonConvert.DeserializeObject(json);

            carpeta.RutaDirectorio = obj.path + "\\";
            carpeta.IDEmpresa = obj.idEmpresa;
            carpeta.IDTipo = obj.idTipo;
            carpeta.IDUsuario = obj.idUsuario;
            carpeta.NombreDirectorio = obj.nombreCarpeta;
            carpeta.NoDocumentos = "0";
        }

        private void Timbrar(Documento doc, Configuracion conf)
        {
            WSCFDI.timbrarCFDI wsTimbrar = new WSCFDI.timbrarCFDI();
            WSCFDI.respuestaTimbrado result = wsTimbrar.CalltimbrarCFDI(conf.SuscriptorRFC, conf.AgenteTI, doc.XmlOrigenBase64);

            //WSEdifact.timbrarCFDI wsTimbrar = new WSEdifact.timbrarCFDI();
            //WSEdifact.respuestaTimbrado result = wsTimbrar.CalltimbrarCFDI(conf.SuscriptorRFC, conf.AgenteTI, doc.XmlOrigenBase64);

            doc.Description = result.codigoDescripcion;
            doc.XmlResultBase64 = result.documentoTimbrado;
            doc.CodigoResultado = result.codigoResultado;
            doc.XmlResultString = DecodeFromBase64(doc.XmlResultBase64);

        }

        private void SimularTimbrar(Documento doc, Configuracion conf)
        {
            doc.Description = "timbrado";
            doc.XmlResultBase64 = EncodeToBase64(LoadDocument(@"C:\Users\Hp\Documents\aaaa\archivotim.xml"));
            doc.CodigoResultado = "100";
            doc.XmlResultString = DecodeFromBase64(doc.XmlResultBase64);
            WriteLog("Archivo Timbrado:" + doc.NombreXml);
        }

        private void CreatePDF(string json, Documento doc, Configuracion conf)
        {
            string jsonTemplate = "{ \"template\": { \"name\" : \"timbrado_rpt\" },\"data\" : " + json + "}";

            HTTPManager pdf = new HTTPManager();
            pdf.RequestPDF(conf.ReportUrl, jsonTemplate, doc.RutaArchivoPDF);
            WriteLog("Archivo PDF Creado para:" + doc.NombreXml);
        }

        private void UpdateProcessedDirectory(Carpeta carpeta)
        {
            SqlParameter[] parameterList = { new SqlParameter("@idNomina", carpeta.IDNomina) };

            SqlServer BaseNomina = new SqlServer();
            BaseNomina.ConnectionString = ConfigurationManager.ConnectionStrings["cnxBaseNomina"].ToString();
            BaseNomina.ExecuteQueryProcedure("UPD_TIMBRE_EXITO_SP", parameterList);
        }

        private void DeleteStampFile(Documento docOrigen)
        {
            File.Delete(docOrigen.RutaArchivoCadenaOriginalB64);
            File.Delete(docOrigen.RutaArchivoFirma);
            File.Delete(docOrigen.RutaArchivoCadenaOriginal);
        }

        private void SetEmptyInsertParams(Documento doc)
        {
            InsertParams param = new InsertParams();

            param.Fecha = (DateTime?)null;
            param.Total = 0;
            param.Nombre = string.Empty;
            param.Descripcion = string.Empty;
            param.FechaPago = null;
            param.Departamento = string.Empty;
            param.NumEmpleado = string.Empty;
            param.Puesto = string.Empty;

            doc.Parametros = param;
        }


        private string InsertProcessedDirectory(Carpeta carpeta)
        {
            SqlParameter[] parameterList = {
            new SqlParameter("@idEmpresa",carpeta.IDEmpresa ),
            new SqlParameter("@idTipo ", carpeta.IDTipo),
            new SqlParameter("@idUsuario",carpeta.IDUsuario),
            new SqlParameter("@nombre",carpeta.NombreDirectorio ),
            new SqlParameter("@ruta",carpeta.RutaDirectorio),
            new SqlParameter("@recibos",carpeta.NoDocumentos)
                                           };
            SqlServer BaseNomina = new SqlServer();
            BaseNomina.ConnectionString = ConfigurationManager.ConnectionStrings["cnxBaseNomina"].ToString();

            return BaseNomina.ExecuteQueryProcedure("INS_TIMBRADO_CARPETA_SP", parameterList).Tables[0].Rows[0][0].ToString();
        }

        private bool ValidateXML(string xmlPath, Configuracion conf)
        {
            XMLManager xml = new XMLManager();
            return xml.Validate(xmlPath, conf.XsdPath, conf.NameSpace);
        }

        private string TransformXml(string xmlPath, string xsltPath)
        {
            XMLManager xml = new XMLManager();
            return xml.XMLToLayout(xmlPath, xsltPath);
        }

        private int ExcuteCommandLine(string programPath, string argsString)
        {
            var process = Process.Start(programPath, argsString);
            process.WaitForExit();
            return process.ExitCode;
        }

        private bool IsAlreadyProcessed(string filePath)
        {
            SqlServer BaseNomina = new SqlServer();
            BaseNomina.ConnectionString = ConfigurationManager.ConnectionStrings["cnxBaseNomina"].ToString();
            SqlParameter[] parameterList = { new SqlParameter("@ruta", filePath) };
            return bool.Parse(BaseNomina.ExecuteQueryProcedure("SEL_TIMBRADO_SP", parameterList).Tables[0].Rows[0][0].ToString());
        }

        private string GetLastDirectory(string directoryPath)
        {
            string[] splitPath = directoryPath.Split('\\');
            return splitPath[splitPath.Length - 1];
        }

        private bool JsonIsvalid(string json)
        {
            bool isValid = false;
            try
            {
                dynamic obj = JsonConvert.DeserializeObject(json);
                isValid = true;
            }
            catch { isValid = false; }

            return isValid;
        }

        private string LoadDocument(string xmlPath)
        {
            IOManager document = new IOManager();
            document.Path = xmlPath;
            document.SetContent();
            return document.Content.Replace("<cfdi:Impuestos />", "<cfdi:Impuestos/>");
        }

        private string EncodeToBase64(string document)
        {
            StringManager encode = new StringManager();
            return encode.ToBase64Encode(document);
        }

        private string DecodeFromBase64(string document)
        {
            StringManager encode = new StringManager();
            return encode.ToBase64Decode(document);
        }

        private void WriteLog(string content, string status)
        {
            string path = _LogPath;
            using (StreamWriter file = new StreamWriter(path, true))
            {
                file.WriteLine("{0}|{1}|{2}", status, DateTime.Now.ToString(), content);
            }
        }

        private void WriteLog(string content)
        {
            string path = _LogPath;
            using (StreamWriter file = new StreamWriter(path, true))
            {
                file.WriteLine("MSG|{0}|{1}", DateTime.Now.ToString(), content);
            }
        }

        private void CreateLogFile(string path)
        {
            if (!File.Exists(path))
            {
                using (FileStream fs = File.Create(path))
                {
                    Byte[] info = new UTF8Encoding(true).GetBytes("Inicio archivo log \n");
                    fs.Write(info, 0, info.Length);
                }
            }
            Console.WriteLine("Creando log........");
            System.Threading.Thread.Sleep(3000);
        }

        private void StartStamp(string data)
        {
            Carpeta carpetaTimbrar = new Carpeta();
            ConvertJsonToClass(data, carpetaTimbrar);

            if (!Directory.Exists(carpetaTimbrar.RutaDirectorio))
            {
                Console.WriteLine("No existe la carpeta:" + carpetaTimbrar.RutaDirectorio);
                return;
            }

            _LogPath = string.Format("{0}\\Logs\\log{1}{2}.txt", Directory.GetCurrentDirectory(),
                carpetaTimbrar.IDEmpresa.ToString(), carpetaTimbrar.NombreDirectorio);

            CreateLogFile(_LogPath);

            Console.WriteLine(carpetaTimbrar);

            if (IsAlreadyProcessed(carpetaTimbrar.RutaDirectorio))
            {
                string msg = string.Format("La carpeta {0} ya fue procesada", carpetaTimbrar.NombreDirectorio);
                Console.WriteLine(msg);
                WriteLog(msg);
                return;
            }

            Configuracion configuraTimbrado = new Configuracion();
            SetConfigurationFromFile(configuraTimbrado);
            SetConfigurationDataBase(configuraTimbrado, carpetaTimbrar);

            CreateDirectory(carpetaTimbrar.OkPath);
            CreateDirectory(carpetaTimbrar.BadPath);

            string[] fileEntries = Directory.GetFiles(carpetaTimbrar.RutaDirectorio, "*.xml");
            carpetaTimbrar.NoDocumentos = fileEntries.Count().ToString();
            carpetaTimbrar.IDNomina = InsertProcessedDirectory(carpetaTimbrar);

            Console.WriteLine("Comienza timbrado de carpeta:" + carpetaTimbrar.NombreDirectorio);
            WriteLog("Comienza timbrado de carpeta:" + carpetaTimbrar.NombreDirectorio, "STR");
            WriteLog("Carpeta contiene:" + carpetaTimbrar.NoDocumentos + " archivos.");
            ProcessFile(carpetaTimbrar, configuraTimbrado, ref  fileEntries, 0);
            Console.WriteLine("termina  timbrado de carpeta:" + carpetaTimbrar.NombreDirectorio);
            WriteLog("Termina  timbrado de carpeta:" + carpetaTimbrar.NombreDirectorio, "END");
        }

        private void CreateDirectory(string originalPath)
        {
            StringBuilder path = new StringBuilder();
            string[] splitPath = originalPath.Split('\\');

            foreach (string s in splitPath)
            {
                try
                {
                    path.Append(s + "\\");

                    if (Directory.Exists(path.ToString())) continue;

                    DirectoryInfo di = Directory.CreateDirectory(path.ToString());
                    WriteLog("directorio creado:" + path.ToString());
                }
                catch (Exception e)
                {
                    WriteLog(string.Format("The process failed: {0}", e.ToString()));
                }

            }
        }

        private void ChangeDocumentDate(string directoryPath)
        {
            string[] fileEntries = Directory.GetFiles(directoryPath);

            foreach (string path in fileEntries)
            {
                try
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(path);
                    string fileName = Path.GetFileNameWithoutExtension(path);

                    XmlElement root = doc.DocumentElement;
                    root.SetAttribute("fecha", "2017-01-01T00:20:29");
                    Console.WriteLine("documento {0} Fecha Cambiada", fileName);
                    doc.Save(path);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("documento err: {0} ", ex.Message);
                }
            }
        }

        private void DeleteQrFiles(Configuracion conf)
        {
            string[] fileEntries = Directory.GetFiles(conf.QrPath);
            foreach (string fileName in fileEntries)
            {
                DateTime creation = File.GetCreationTime(fileName);
                if (creation < DateTime.Now.AddHours(-1))
                {
                    File.Delete(fileName);
                }
            }        
        }

    }
}