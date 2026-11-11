namespace Finance.Locales {
    using System;
    
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class Locale {
        
        private static global::System.Resources.ResourceManager resourceMan;
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Locale() {
        }
        
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Finance.Locales.Locale", typeof(Locale).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        public static string Finance {
            get {
                return ResourceManager.GetString("Finance", resourceCulture);
            }
        }
        
        public static string Finance_Crypto_Title {
            get {
                return ResourceManager.GetString("Finance_Crypto_Title", resourceCulture);
            }
        }
        
        public static string Finance_Crypto_Subtitle {
            get {
                return ResourceManager.GetString("Finance_Crypto_Subtitle", resourceCulture);
            }
        }
        
        public static string Finance_Stocks_Title {
            get {
                return ResourceManager.GetString("Finance_Stocks_Title", resourceCulture);
            }
        }
        
        public static string Finance_Stocks_Subtitle {
            get {
                return ResourceManager.GetString("Finance_Stocks_Subtitle", resourceCulture);
            }
        }
        
        public static string Finance_Converter_Title {
            get {
                return ResourceManager.GetString("Finance_Converter_Title", resourceCulture);
            }
        }
        
        public static string Finance_Converter_Subtitle {
            get {
                return ResourceManager.GetString("Finance_Converter_Subtitle", resourceCulture);
            }
        }
    }
}
