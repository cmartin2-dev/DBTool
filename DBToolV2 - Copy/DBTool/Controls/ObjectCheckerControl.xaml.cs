using DocumentFormat.OpenXml.Office2013.Word;
using Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DBTool.Controls
{
    /// <summary>
    /// Interaction logic for ObjectCheckerControl.xaml
    /// </summary>
    public partial class ObjectCheckerControl : UserControl
    {
        List<Locale> locales = new List<Locale>();
        List<CustomType> customObjectTypes = null;
        List<ERDObjectChecker> lstErdObjectCheckers = null;
        public ObjectCheckerControl()
        {
            InitializeComponent();
            lstObjecResult.AddContextMenu();
            if (File.Exists($"LOCALE\\LOCALE.json"))
            {
                string localeStr = File.ReadAllText($"LOCALE\\LOCALE.json");
                locales = JsonConvert.DeserializeObject<List<Locale>>(localeStr);
            }
        }

        public void SetDataContext(CustomType customType,List<CustomType> _customObjectTypes)
        {
            this.DataContext = customType;
            customObjectTypes = _customObjectTypes;
            GetEntityProperties(customType);

        }

        private void btnCheckDuplicate_Click(object sender, RoutedEventArgs e)
        {
            if (lstErdObjectCheckers != null)
            {
                CustomType customType = this.DataContext as CustomType;

                var duplicateNames = lstErdObjectCheckers.Where(x=>!string.IsNullOrEmpty(x.ColumnName))
                 .GroupBy(p => p.ColumnName)
                 .Where(g => g.Count() > 1)
                 .Select(g => g.Key)
                 .ToHashSet();

                lstObjecResult.collectionView.Filter = item =>
                {
                    var i = item as ERDObjectChecker;
                    return duplicateNames.Contains(i.ColumnName);
                };

                lstObjecResult.collectionView.Refresh();
            }
        }

        private void GetEntityProperties(CustomType entity)
        {
            ProcessTableNav(entity);

            lstObjecResult.CustomColumns.Clear();
            lstObjecResult.CustomColumns.Add("EntityName", "Entity Property Name");
            lstObjecResult.CustomColumns.Add("ColumnName", "Database Column Name");
            lstObjecResult.CustomColumns.Add("TableName", "Database Table Name");
            lstObjecResult.CustomColumns.Add("RelationalKey", "Foreign Key");
            lstObjecResult.CustomColumns.Add("LocaleKey", "Locale Key");
            lstObjecResult.CustomColumns.Add("LocaleValue", "Locale Value");

        lstErdObjectCheckers = new List<ERDObjectChecker>();

            foreach (var column in entity.Columns)
            {
                ERDObjectChecker erdColumn = new ERDObjectChecker();
                erdColumn.ColumnName = column.ColumnName;
                erdColumn.EntityName = column.EntityPropertyName;

                var locale = locales.FirstOrDefault(x => x.Key.ToLower() == $"{entity.OriginalEntity.ToLower()}.{column.EntityPropertyName.ToLower()}.displaytext");

                if (locale != null)
                {
                    erdColumn.LocaleKey = locale.Key;
                    erdColumn.LocaleValue = locale.Value;
                }

                lstErdObjectCheckers.Add(erdColumn);
            }

            foreach(var nav in entity.NavigationProperty)
            {
                ERDObjectChecker erdColumn = new ERDObjectChecker();
                erdColumn.EntityName = nav.OriginalEntity;
                erdColumn.TableName = nav.TableName;
                erdColumn.RelationalKey = nav.ForeignKey;


                lstErdObjectCheckers.Add(erdColumn);
            }

            lstObjecResult.LoadData(lstErdObjectCheckers,width:300);
            

        }

        private void ProcessTableNav(CustomType customType, int level = 0)
        {

            foreach (var table in customType.NavigationProperty)
            {
                var asdasf = customObjectTypes.Where(x => x.ObjectType == table.ObjectType).ToList();

                var eee = customObjectTypes.FirstOrDefault(x => x.ObjectType == table.ObjectType);
                if (eee != null)
                {
                    //if (tableEntities.Contains(eee))
                    //    continue;

                    CustomType newItem = new CustomType();
                    newItem.Columns = eee.Columns;
                    newItem.ForeignKey = eee.ForeignKey;
                    newItem.NavigationProperty = eee.NavigationProperty;
                    newItem.TableName = eee.TableName;
                    newItem.ObjectType = eee.ObjectType;
                    newItem.OriginalEntity = eee.OriginalEntity;
                    newItem.ParentType = customType.ObjectType;


                    //var exists = tableEntities.FirstOrDefault(x => x.ObjectType == newItem.ObjectType && x.ParentType == newItem.ParentType);
                    //if (exists == null)
                    //{
                    //    tableEntities.Add(newItem);
                    //    ProcessTableNav(newItem);
                    //}
                }
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (lstErdObjectCheckers != null)
            {
                lstObjecResult.collectionView.Filter = null;
            }
        }
    }
}
