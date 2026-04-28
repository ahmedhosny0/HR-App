namespace CK.ViewModel
{
    public class ConnectionDB
    {
        public string Server { get; set; }
        public string AxdbConnection = string.Format("Server=192.168.1.210;User ID=sa;Password=P@ssw0rd;Database=AXDB;Connect Timeout=10200;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False;");
        public string RichCutConnection = string.Format("Server=192.168.111.2;User ID=sa;Password=P@ssw0rd;Database=Richcut;Connect Timeout=10200;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False;");
        public string RmsConnection = string.Format("Server=192.168.1.156;User ID=sa;Password=S@leh2024;Database=DATA_CENTER;Connect Timeout=10200;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False;");
        public string RmsBeforeConnection = string.Format("Server=192.168.1.156;User ID=sa;Password=S@leh2024;Database=DATA_CENTER_Prev_Yrs;Connect Timeout=10200;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False;");
        public string TopSoftConnection = string.Format("Server=192.168.1.208;User ID=sa;Password=P@ssw0rd123;Database=TopSoft;Connect Timeout=10200;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False;");
        public string EasySoftConnection = string.Format("Server=192.168.1.76\\sql2016;User ID=mohamed;Password=P@ssw0rd12345;Database=Easysoft;Connect Timeout=10200;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False;");
        public string CloudConnection = "Server=tcp:d365fo-ck.database.windows.net,1433;" +
                                        "Authentication=Active Directory Password;" +
                                        "Database=AXDB_BYOD;" +
                                        "User ID=MFA@circlekegypt.com;" +
                                        "Password=TMT@2025;" +
                                        "Encrypt=True;" +
                                        "TrustServerCertificate=False;" +
                                        "Connection Timeout=30;";
    }
}
