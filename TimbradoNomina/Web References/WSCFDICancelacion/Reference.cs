﻿//------------------------------------------------------------------------------
// <auto-generated>
//     Este código fue generado por una herramienta.
//     Versión de runtime:4.0.30319.42000
//
//     Los cambios en este archivo podrían causar un comportamiento incorrecto y se perderán si
//     se vuelve a generar el código.
// </auto-generated>
//------------------------------------------------------------------------------

// 
// Microsoft.VSDesigner generó automáticamente este código fuente, versión=4.0.30319.42000.
// 
#pragma warning disable 1591

namespace TimbradoNomina.WSCFDICancelacion {
    using System;
    using System.Web.Services;
    using System.Diagnostics;
    using System.Web.Services.Protocols;
    using System.Xml.Serialization;
    using System.ComponentModel;
    
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.7.2053.0")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Web.Services.WebServiceBindingAttribute(Name="enviaAcuseCancelacionBinding", Namespace="http://edifact.com.mx/xsd")]
    public partial class enviaAcuseCancelacion : System.Web.Services.Protocols.SoapHttpClientProtocol {
        
        private System.Threading.SendOrPostCallback CallenviaAcuseCancelacionOperationCompleted;
        
        private bool useDefaultCredentialsSetExplicitly;
        
        /// <remarks/>
        public enviaAcuseCancelacion() {
            this.Url = global::TimbradoNomina.Properties.Settings.Default.TimbradoNomina_WSCFDICancelacion_enviaAcuseCancelacion;
            if ((this.IsLocalFileSystemWebService(this.Url) == true)) {
                this.UseDefaultCredentials = true;
                this.useDefaultCredentialsSetExplicitly = false;
            }
            else {
                this.useDefaultCredentialsSetExplicitly = true;
            }
        }
        
        public new string Url {
            get {
                return base.Url;
            }
            set {
                if ((((this.IsLocalFileSystemWebService(base.Url) == true) 
                            && (this.useDefaultCredentialsSetExplicitly == false)) 
                            && (this.IsLocalFileSystemWebService(value) == false))) {
                    base.UseDefaultCredentials = false;
                }
                base.Url = value;
            }
        }
        
        public new bool UseDefaultCredentials {
            get {
                return base.UseDefaultCredentials;
            }
            set {
                base.UseDefaultCredentials = value;
                this.useDefaultCredentialsSetExplicitly = true;
            }
        }
        
        /// <remarks/>
        public event CallenviaAcuseCancelacionCompletedEventHandler CallenviaAcuseCancelacionCompleted;
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapRpcMethodAttribute("http://comprobantes-fiscales.com/service/cancelarCFDI.php/enviaAcuseCancelacion", RequestNamespace="http://edifact.com.mx/xsd", ResponseNamespace="http://edifact.com.mx/xsd")]
        [return: System.Xml.Serialization.SoapElementAttribute("ns1:return")]
        public string CallenviaAcuseCancelacion(string xmlFile) {
            object[] results = this.Invoke("CallenviaAcuseCancelacion", new object[] {
                        xmlFile});
            return ((string)(results[0]));
        }
        
        /// <remarks/>
        public void CallenviaAcuseCancelacionAsync(string xmlFile) {
            this.CallenviaAcuseCancelacionAsync(xmlFile, null);
        }
        
        /// <remarks/>
        public void CallenviaAcuseCancelacionAsync(string xmlFile, object userState) {
            if ((this.CallenviaAcuseCancelacionOperationCompleted == null)) {
                this.CallenviaAcuseCancelacionOperationCompleted = new System.Threading.SendOrPostCallback(this.OnCallenviaAcuseCancelacionOperationCompleted);
            }
            this.InvokeAsync("CallenviaAcuseCancelacion", new object[] {
                        xmlFile}, this.CallenviaAcuseCancelacionOperationCompleted, userState);
        }
        
        private void OnCallenviaAcuseCancelacionOperationCompleted(object arg) {
            if ((this.CallenviaAcuseCancelacionCompleted != null)) {
                System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
                this.CallenviaAcuseCancelacionCompleted(this, new CallenviaAcuseCancelacionCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
            }
        }
        
        /// <remarks/>
        public new void CancelAsync(object userState) {
            base.CancelAsync(userState);
        }
        
        private bool IsLocalFileSystemWebService(string url) {
            if (((url == null) 
                        || (url == string.Empty))) {
                return false;
            }
            System.Uri wsUri = new System.Uri(url);
            if (((wsUri.Port >= 1024) 
                        && (string.Compare(wsUri.Host, "localHost", System.StringComparison.OrdinalIgnoreCase) == 0))) {
                return true;
            }
            return false;
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.7.2053.0")]
    public delegate void CallenviaAcuseCancelacionCompletedEventHandler(object sender, CallenviaAcuseCancelacionCompletedEventArgs e);
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.7.2053.0")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class CallenviaAcuseCancelacionCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs {
        
        private object[] results;
        
        internal CallenviaAcuseCancelacionCompletedEventArgs(object[] results, System.Exception exception, bool cancelled, object userState) : 
                base(exception, cancelled, userState) {
            this.results = results;
        }
        
        /// <remarks/>
        public string Result {
            get {
                this.RaiseExceptionIfNecessary();
                return ((string)(this.results[0]));
            }
        }
    }
}

#pragma warning restore 1591