namespace ProductivityApi.Models.PLF
{
    public class CollateralApiRequest
    {
        public string collateralRef { get; set; } = string.Empty;
    }
    public class CollateralApiResponse
    {
        public string cif { get; set; } = string.Empty;
        public string collateralRef { get; set; } = string.Empty;
        public string collateralType { get; set; } = string.Empty;
        public string collateralCcy { get; set; } = string.Empty;
        public decimal maxCollateralValue { get; set; }
        public decimal splLtv { get; set; }
        public string collateralLoc { get; set; } = string.Empty;
        public string valuator { get; set; } = string.Empty;
        public bool alwMultiLoanAssign { get; set; }
        public bool alwThirdPartyAssign { get; set; }
        public List<object> thirdParties { get; set; } = new();
        public List<ExtendedDetail> extendedDetails { get; set; } = new();
        public List<object> coOwnerDetails { get; set; } = new();
        public List<object> documents { get; set; } = new();
        public ReturnStatus returnStatus { get; set; } = new();
        public int version { get; set; }
        public List<ExtendedFieldRenderItem> extendedFieldRenderList { get; set; } = new();
        public string areaLocalityName { get; set; } = string.Empty;
        public string stateName { get; set; } = string.Empty;
        public string cityName { get; set; } = string.Empty;
    }
    public class ExtendedDetail
    {
        public List<ExtendedField> extendedFields { get; set; } = new();
    }
    public class ExtendedField
    {
        public string fieldName { get; set; } = string.Empty;
        public object? fieldValue { get; set; }
    }
    public class ExtendedFieldRenderItem
    {
        public int version { get; set; }
        public CollateralMapValues mapValues { get; set; } = new();
    }
    public class CollateralMapValues
    {
        public int noofunits { get; set; }
        public decimal unitprice { get; set; }
        public string addresslineonecoll { get; set; } = string.Empty;
        public string addresslinetwocoll { get; set; } = string.Empty;
        public string citycollateral { get; set; } = string.Empty;
        public string advocatecodelegal { get; set; } = string.Empty;
        public int postalcodecollater { get; set; }
        public string remarkscollateral { get; set; } = string.Empty;
        public string statecollateral { get; set; } = string.Empty;
        public string collateraltype { get; set; } = string.Empty;
        public string proponeinitvendone { get; set; } = string.Empty;
        public string vendorcodepovo { get; set; } = string.Empty;
        public decimal oneflrsqftpovo { get; set; }
        public decimal oneflrvalpovo { get; set; }
        public decimal twoflrsqftpovo { get; set; }
        public decimal twoflrvalpovo { get; set; }
        public decimal threeflrsqftpovo { get; set; }
        public decimal threeflrvalpovo { get; set; }
        public decimal fourflrsqftpovo { get; set; }
        public decimal fourflrvalpovo { get; set; }
        public decimal fiveflrsqftpovo { get; set; }
        public decimal fivevalprsqftpovo { get; set; }
        public string accesstoproppovo { get; set; } = string.Empty;
        public int ageofpropconstpovo { get; set; }
        public string relationshippovo { get; set; } = string.Empty;
        public string colschproponevalon { get; set; } = string.Empty;
        public decimal comppercpovo { get; set; }
        public string curoccproponevalon { get; set; } = string.Empty;
        public DateTime? dtofpropvisitpovo { get; set; }
        public string detailsofsurrpovo { get; set; } = string.Empty;
        public int distancepovo { get; set; }
        public string districtpovo { get; set; } = string.Empty;
        public DateTime? docdatepovo { get; set; }
        public string docprovidedpovo { get; set; } = string.Empty;
        public decimal groundvalprsqftpov { get; set; }
        public string eastverpovo { get; set; } = string.Empty;
        public string eastdocpovo { get; set; } = string.Empty;
        public decimal loanelicollltvpovo { get; set; }
        public decimal fairmrktvalpovo { get; set; }
        public string finalrecommpovo { get; set; } = string.Empty;
        public string flooringdescrpovo { get; set; } = string.Empty;
        public string forceddistresspovo { get; set; } = string.Empty;
        public decimal groundflrsqftpovo { get; set; }
        public decimal groundflrvalpovo { get; set; }
        public string otherissuepovo { get; set; } = string.Empty;
        public string jointownershippovo { get; set; } = string.Empty;
        public decimal landareadocpovo { get; set; }
        public decimal landareaverpovo { get; set; }
        public decimal landvalpersqftpovo { get; set; }
        public decimal landbuildvalpovo { get; set; }
        public string landmarkpovo { get; set; } = string.Empty;
        public string deviationfta { get; set; } = string.Empty;
        public decimal loanallowpropone { get; set; }
        public decimal requestedloanamoun { get; set; }
        public string localitypovo { get; set; } = string.Empty;
        public decimal ltvrequestedpovo { get; set; }
        public decimal loanelimaxloanpovo { get; set; }
        public decimal maxloancollpovo { get; set; }
        public int maxltvallowcollpov { get; set; }
        public int numbreoffloorspovo { get; set; }
        public int nooftenantspovo { get; set; }
        public int noofroomspovo { get; set; }
        public string northverpovo { get; set; } = string.Empty;
        public string northdocpovo { get; set; } = string.Empty;
        public string propcatchmentpovo { get; set; } = string.Empty;
        public string personmetpovo { get; set; } = string.Empty;
        public int pincodepovo { get; set; }
        public string planapprpovo { get; set; } = string.Empty;
        public string plotidpovo { get; set; } = string.Empty;
        public string propownerspovo { get; set; } = string.Empty;
        public string propsubtypepovo { get; set; } = string.Empty;
        public string qualbyvaluerpovo { get; set; } = string.Empty;
        public int residualagepovo { get; set; }
        public string roadextensionpovo { get; set; } = string.Empty;
        public string roofdescpovo { get; set; } = string.Empty;
        public string propsepentrancepov { get; set; } = string.Empty;
        public string southverpovo { get; set; } = string.Empty;
        public string southdocpovo { get; set; } = string.Empty;
        public decimal sqftaffectedpovo { get; set; }
        public string statepovo { get; set; } = string.Empty;
        public string stateproponevalone { get; set; } = string.Empty;
        public string streetnamepovo { get; set; } = string.Empty;
        public string survnopovo { get; set; } = string.Empty;
        public string talukpovo { get; set; } = string.Empty;
        public decimal totalvalprsqftpovo { get; set; }
        public decimal totalbuiltupareapo { get; set; }
        public decimal totallandvaluepovo { get; set; }
        public string typeofconspovo { get; set; } = string.Empty;
        public string typeofdocpovo { get; set; } = string.Empty;
        public DateTime? valuationreportdat { get; set; }
        public string villagepovo { get; set; } = string.Empty;
        public string walldescpovo { get; set; } = string.Empty;
        public string westverpovo { get; set; } = string.Empty;
        public string westdocpovo { get; set; } = string.Empty;
        public int widthroadpovo { get; set; }
        public decimal totalbuildvalpovo { get; set; }

        // Legal fields
        public string advocatcodelegal { get; set; } = string.Empty;
        public string advocatename { get; set; } = string.Empty;
        public string buyerlegal { get; set; } = string.Empty;
        public string collateraloneid { get; set; } = string.Empty;
        public DateTime? deedexecutionlegal { get; set; }
        public string deputyreglegal { get; set; } = string.Empty;
        public string distrcitlegal { get; set; } = string.Empty;
        public string eastlegal { get; set; } = string.Empty;
        public string mortgagekindlegal { get; set; } = string.Empty;
        public decimal landarealegal { get; set; }
        public DateTime? legalreportdate { get; set; }
        public string northlegal { get; set; } = string.Empty;
        public int pincodelegal { get; set; }
        public string propertyownerslega { get; set; } = string.Empty;
        public string relationappllegal { get; set; } = string.Empty;
        public string sellernamelegal { get; set; } = string.Empty;
        public string southlegal { get; set; } = string.Empty;
        public string surveylegal { get; set; } = string.Empty;
        public string taluklegal { get; set; } = string.Empty;
        public string typedeedlegal { get; set; } = string.Empty;
        public string villagelegal { get; set; } = string.Empty;
        public string westlegal { get; set; } = string.Empty;
    }
}
