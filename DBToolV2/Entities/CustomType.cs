using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class CustomType
    {
        public CustomType()
        {
            Columns = new List<ColumnIdentity>();
            NavigationProperty = new List<CustomType>();
        }

        public string TableName { get; set; }

        public string OriginalEntity { get; set; }

        public Type ObjectType { get; set; }

        public Type ParentType { get; set; }

        public List<ColumnIdentity> Columns { get; set; }

        public List<CustomType> NavigationProperty { get; set; }

        public string ForeignKey { get; set; }

        public string MetaData { get; set; }
       
    }

    public class ColumnIdentity
    {
        public ColumnIdentity()
        {
        }

        public string ColumnName { get; set; }
        public bool IsLookUp { get; set; }
        public string LookUpName { get; set; }

        public string EntityPropertyName { get; set; }
        public string DBColumnName { get; set; }

        public string MetaData {  get; set; }

        public string LocaleKey { get; set; }

        public string LocaleValue { get; set; }
    }

    public class GenericLookUpMapper
    {
        public GenericLookUpMapper()
        {
            GenericLookUpList = new List<GenericLookUp>();  
        }
        public List<GenericLookUp> GenericLookUpList { get; set; }
    }

    public class GenericLookUp
    {
        public int Id { get; set; }
        public string TableName { get; set; }

        public string MappedName { get; set; }
    }
}
