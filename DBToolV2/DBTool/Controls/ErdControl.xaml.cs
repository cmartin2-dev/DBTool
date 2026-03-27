using DocumentFormat.OpenXml.Bibliography;
using Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Reflection;
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
    /// Interaction logic for ErdControl.xaml
    /// </summary>
    public partial class ErdControl : UserControl
    {

        private Type EntityLookupTableMap;
        private Type GenericLookUpType;
        private Type LibraryType;
        List<CustomType> customObjectTypes = new List<CustomType>();

        List<Assembly> lstAssemblies = new List<Assembly>();

        GenericLookUpMapper GLMapperList = new GenericLookUpMapper();
        List<ListViewItem> allItems = new List<ListViewItem>();

        public ErdControl()
        {
            InitializeComponent();
            lstEntityResults.AddContextMenu();
        }

        private void btnLoadDLL_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                GLMapperList = new GenericLookUpMapper();
                customObjectTypes = new List<CustomType>();


                string dllDirectory = @"FPLMDLL";
                if (!Directory.Exists(dllDirectory))
                {
                    ThemedDialog.Show("Missing DLL directory.", "ERROR");
                    return;
                }

                //     List<Assembly> lstAssemblies = new List<Assembly>();

                foreach (string dllFile in Directory.GetFiles(dllDirectory, "*.dll"))
                {

                    try
                    {
                        Assembly assembly = Assembly.LoadFrom(dllFile);
                        if (!lstAssemblies.Contains(assembly))
                            lstAssemblies.Add(assembly);
                        // List<Type> customTypes = assembly.GetTypes().Where(x => !x.IsInterface).OrderBy(x => x.Name).ToList();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to load {dllFile}: {ex.Message}");
                    }

                }

                List<string> tables = new List<string>();
                List<string> errorTypes = new List<string>();

                foreach (Assembly assembly in lstAssemblies)
                {
                    try
                    {
                        List<Type> customTypes = assembly.GetTypes().Where(x => !x.IsInterface).OrderBy(x => x.Name).ToList();

                     var names =   customTypes.Select(x => x.Name).ToList(); 

                        if (customTypes != null && customTypes.Count > 0)
                        {
                            foreach (Type type in customTypes)
                            {

                                try
                                {
                                    if (type.Name == "EntityLookupTableMap")
                                    {
                                        EntityLookupTableMap = type;
                                        continue;
                                    }
                                    if (type.Name == "GenericLookupType")
                                    {
                                        GenericLookUpType = type;
                                        continue;
                                    }
                                    if (type.Name == "LibraryType")
                                    {
                                        LibraryType = type;
                                        continue;
                                    }
                                    if (type.Name.ToLower() == "style")
                                    {
                                        CustomType customType = new CustomType();
                                        customType.TableName = type.Name;
                                        customType.ObjectType = type;
                                        customType.OriginalEntity = type.Name;

                                        customObjectTypes.Add(customType);
                                        continue;
                                    }

                                    var tableAttribute = type.CustomAttributes.FirstOrDefault(x => x.AttributeType == typeof(TableAttribute));
                                    if (tableAttribute != null)
                                    {
                                        var tableName = tableAttribute.ConstructorArguments.Select(x => x.Value).FirstOrDefault();
                                        if (tableName != null)
                                        {
                                            CustomType customType = new CustomType();
                                            customType.TableName = tableName.ToString();
                                            customType.ObjectType = type;
                                            customType.OriginalEntity = type.Name;

                                            customObjectTypes.Add(customType);
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    errorTypes.Add(type.Name);
                                }

                            }
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                }


                var items = customObjectTypes.OrderBy(x => x.TableName).ToList();
                GetLookUpTable();
                foreach (var item in items)
                {
                    ProcessCustomObject(item);
                }

                var navlist = items.Select(x => x.NavigationProperty).Where(x => x.Count() > 0).ToList();

                foreach (var item in navlist)
                {
                    foreach (var item2 in item)
                    {
                        var exists = customObjectTypes.FirstOrDefault(x => x.TableName == item2.TableName);
                        if (exists != null)
                            continue;

                        ProcessCustomObject(item2);
                        items.Add(item2);
                    }
                }


                customObjectTypes.AddRange(items);

                foreach (var item in customObjectTypes)
                {
                     ProcessAllNavigation(item);
                }
                customObjectTypes = customObjectTypes.OrderBy(x => x.TableName).ToList();



                List<CustomType> cleanedList = new List<CustomType>();

                foreach (var item in customObjectTypes)
                {
                    if(item.OriginalEntity.ToLower() =="caregroup" && item.Columns.Count() > 0)
                    {

                    }
                    var exists = cleanedList.FirstOrDefault(x => x.TableName.ToLower() == item.TableName.ToLower() && x.OriginalEntity.ToLower() == item.OriginalEntity.ToLower());
                    if (exists != null)
                        continue;

                    if(item.Columns.Count == 0)
                        ProcessProperty(item);

                    cleanedList.Add(item);

                }

                customObjectTypes.Clear();
                customObjectTypes.AddRange(cleanedList);
                lstEntityResults.CustomColumns.Clear();

                lstEntityResults.CustomColumns.Add("TableName", "Database Table Name");
                lstEntityResults.CustomColumns.Add("OriginalEntity", "Entity Name");

                lstEntityResults.LoadData(customObjectTypes, width: 150);



                //lstViewTable.Columns.Clear();
                //lstViewTable.Columns.Add("Entity", 200);

                //lstViewTable.Columns.Add("Original Entity Name", 200);
                //lstViewTable.AddListViewData(customObjectTypes);

            }
            catch (Exception ex)
            {

            }
        }

        private void GetLookUpTable()
        {
            // Get the private constructor
            ConstructorInfo ctor = EntityLookupTableMap.GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                Type.EmptyTypes,
                null);


            Dictionary<string, string> GenericLookUpMapperDictionary = null;

            if (ctor != null)
            {
                object secretInstance = ctor.Invoke(null);
                PropertyInfo InstanceProperty = EntityLookupTableMap.GetProperty("Instance");

                GenericLookUpMapperDictionary = InstanceProperty.GetValue(secretInstance) as Dictionary<string, string>;

                foreach (var obj in GenericLookUpMapperDictionary)
                {
                    GenericLookUp genericLookUp = new GenericLookUp();

                    string mappedName = obj.Value;

                    var glObj = GenericLookUpType.GetFields().FirstOrDefault(x => x.Name.ToLower() == obj.Key.ToLower());

                    if (glObj != null)
                    {

                        string tableName = glObj.Name;

                        genericLookUp.TableName = tableName;
                        // genericLookUp.Id = id;
                        genericLookUp.MappedName = mappedName;

                        GLMapperList.GenericLookUpList.Add(genericLookUp);
                    }
                    else
                    {
                        var libObj = LibraryType.GetFields().FirstOrDefault(x => x.Name.ToLower() == obj.Key.ToLower());
                        if (libObj != null)
                        {

                            string tableName = libObj.Name;

                            genericLookUp.TableName = tableName;
                            // genericLookUp.Id = id;
                            genericLookUp.MappedName = mappedName;

                            GLMapperList.GenericLookUpList.Add(genericLookUp);
                        }
                    }

                    foreach (Assembly assembly in lstAssemblies)
                    {
                        List<Type> customTypes = assembly.GetTypes().Where(x => !x.IsInterface).OrderBy(x => x.Name).ToList();
                        foreach (Type type in customTypes)
                        {
                            string name = type.Name.ToLower();
                            var lookup = GLMapperList.GenericLookUpList.FirstOrDefault(x => x.MappedName.ToLower() == name.ToLower());
                            if (lookup != null)
                            {
                                CustomType customType = new CustomType();
                                customType.TableName = lookup.TableName;
                                customType.ObjectType = type;
                                customType.OriginalEntity = type.Name;

                                customObjectTypes.Add(customType);
                            }

                        }
                    }
                }

            }
        }

        private void ProcessCustomObject(CustomType customType)
        {
            //get all column property
            var properties = customType.ObjectType.GetProperties();
            if (properties != null && properties.Length > 0)
            {
                if (customType.OriginalEntity.ToLower() == "caregroup")
                {

                }
                ProcessProperty(customType);
            }

        }
        private void ProcessProperty(CustomType customType, bool isNavProperty = false)
        {
            if (customType.OriginalEntity.ToLower() == "caregroup")
            {

            }

            PropertyInfo[] properties = customType.ObjectType.GetProperties();

            foreach (var property in properties)
            {

                ColumnIdentity columnIdentity = new ColumnIdentity();

                if (property.PropertyType.Namespace.ToLower() == "system")
                {
                    columnIdentity.EntityPropertyName = property.Name;
                    columnIdentity.MetaData += property.Name + "&con&";

                    if (property.CustomAttributes != null && property.CustomAttributes.Count() > 0)
                    {
                        var notMappedProperty = property.CustomAttributes.FirstOrDefault(x => x.AttributeType == typeof(NotMappedAttribute));
                        if (notMappedProperty != null)
                        {
                            customType.Columns.Add(columnIdentity);
                            continue;
                        }
                        var DBColumnNameAttribute = property.CustomAttributes.FirstOrDefault(x => x.AttributeType == typeof(ColumnAttribute));
                        if (DBColumnNameAttribute != null)
                        {
                            var DBColumnName = DBColumnNameAttribute.ConstructorArguments.Select(x => x.Value).FirstOrDefault();
                            if (DBColumnName != null)
                            {
                                columnIdentity.ColumnName = DBColumnName.ToString().ToUpper();
                                columnIdentity.DBColumnName = DBColumnName.ToString().ToUpper();
                                columnIdentity.MetaData += columnIdentity.ColumnName + "&con&";
                                columnIdentity.MetaData += columnIdentity.DBColumnName + "&con&";
                            }


                        }
                        else
                        {
                            columnIdentity.DBColumnName = property.Name.ToUpper();
                            columnIdentity.ColumnName = property.Name.ToUpper();

                            columnIdentity.MetaData += columnIdentity.ColumnName + "&con&";
                            columnIdentity.MetaData += columnIdentity.DBColumnName + "&con&";
                        }

                    }
                    else
                    {
                        columnIdentity.DBColumnName = property.Name.ToUpper();
                        columnIdentity.ColumnName = property.Name.ToUpper();


                        columnIdentity.MetaData += columnIdentity.ColumnName + "&con&";
                        columnIdentity.MetaData += columnIdentity.DBColumnName + "&con&";
                    }
                    customType.Columns.Add(columnIdentity);

                }
                else //if navigation property
                {
                    try
                    {
                        if (property.CustomAttributes != null && property.CustomAttributes.Count() > 0)
                        {
                            var notMappedProperty = property.CustomAttributes.FirstOrDefault(x => x.AttributeType == typeof(NotMappedAttribute));
                            if (notMappedProperty != null)
                            {
                                continue;
                            }

                        }

                        //check if list
                        if (property.PropertyType.IsGenericType)
                        {

                            Type genericType = property.PropertyType.GetGenericArguments()[0];
                            if (genericType != null)
                            {
                                //if (genericType.Name.ToLower() == "style")
                                //    return;

                                var item = customObjectTypes.FirstOrDefault(x => x.TableName == genericType.Name);
                                if (item == null)
                                {
                                    var tableAttribute = genericType.CustomAttributes.FirstOrDefault(x => x.AttributeType == typeof(TableAttribute));
                                    if (tableAttribute != null)
                                    {
                                        var tableName = tableAttribute.ConstructorArguments.Select(x => x.Value).FirstOrDefault();
                                        if (tableName != null)
                                        {
                                            CustomType navType = new CustomType();
                                            navType.TableName = tableName.ToString();
                                            navType.ObjectType = genericType;
                                            //   navType.ParentType = customType.ObjectType;
                                            navType.OriginalEntity = genericType.Name;

                                            navType.MetaData += navType.TableName + "&con&";
                                            navType.MetaData += navType.OriginalEntity + "&con&";

                                            var foreignAtt = property.CustomAttributes.FirstOrDefault(x => x.AttributeType == typeof(ForeignKeyAttribute));

                                            if (foreignAtt != null)
                                            {
                                                var fk = foreignAtt.ConstructorArguments.Select(x => x.Value).FirstOrDefault();
                                                navType.ForeignKey = fk.ToString();

                                                navType.MetaData += navType.ForeignKey + "&con&";
                                            }
                                            else // check relationalkey
                                            {
                                                var frelationalAtt = property.CustomAttributes.FirstOrDefault(x => x.AttributeType.Name == "RelationalKeyAttribute");
                                                if (frelationalAtt != null)
                                                {
                                                    var fk = frelationalAtt.ConstructorArguments.Select(x => x.Value).FirstOrDefault();
                                                    navType.ForeignKey = fk.ToString();


                                                    navType.MetaData += navType.ForeignKey + "&con&";
                                                }

                                            }

                                            //if(!isNavProperty)
                                            //ProcessProperty(navType, true);

                                            customType.NavigationProperty.Add(navType);
                                        }
                                    }
                                    else
                                    {
                                        CustomType navType = new CustomType();
                                        navType.TableName = genericType.Name;
                                        navType.ObjectType = genericType;
                                        navType.OriginalEntity = genericType.Name;


                                        navType.MetaData += genericType.Name + "&con&";

                                        navType.MetaData += navType.OriginalEntity + "&con&";

                                        var foreignAtt = property.CustomAttributes.FirstOrDefault(x => x.AttributeType == typeof(ForeignKeyAttribute));

                                        if (foreignAtt != null)
                                        {
                                            var fk = foreignAtt.ConstructorArguments.Select(x => x.Value).FirstOrDefault();
                                            navType.ForeignKey = fk.ToString();


                                            navType.MetaData += navType.ForeignKey + "&con&";
                                        }
                                        else // check relationalkey
                                        {
                                            var frelationalAtt = property.CustomAttributes.FirstOrDefault(x => x.AttributeType.Name == "RelationalKeyAttribute");
                                            if (frelationalAtt != null)
                                            {
                                                var fk = frelationalAtt.ConstructorArguments.Select(x => x.Value).Last();
                                                navType.ForeignKey = fk.ToString();


                                                navType.MetaData += navType.ForeignKey + "&con&";
                                            }

                                        }

                                        customType.NavigationProperty.Add(navType);
                                    }
                                }
                            }
                        }
                        else
                        {
                            var tableAttribute = property.PropertyType.CustomAttributes.FirstOrDefault(x => x.AttributeType == typeof(TableAttribute));
                            if (tableAttribute != null)
                            {
                                var tableName = tableAttribute.ConstructorArguments.Select(x => x.Value).FirstOrDefault();
                                if (tableName != null)
                                {
                                    CustomType navType = new CustomType();
                                    navType.TableName = tableName.ToString();
                                    navType.ObjectType = property.PropertyType;


                                    navType.MetaData += navType.TableName + "&con&";

                                    // navType.ParentType = customType.ObjectType;
                                    navType.OriginalEntity = tableName.ToString();


                                    navType.MetaData += navType.OriginalEntity + "&con&";


                                    var foreignAtt = property.CustomAttributes.FirstOrDefault(x => x.AttributeType == typeof(ForeignKeyAttribute));

                                    if (foreignAtt != null)
                                    {
                                        var fk = foreignAtt.ConstructorArguments.Select(x => x.Value).FirstOrDefault();
                                        navType.ForeignKey = fk.ToString();


                                        navType.MetaData += navType.ForeignKey + "&con&";
                                    }
                                    else // check relationalkey
                                    {
                                        var frelationalAtt = property.CustomAttributes.FirstOrDefault(x => x.AttributeType.Name == "RelationalKeyAttribute");
                                        if (frelationalAtt != null)
                                        {
                                            var fk = frelationalAtt.ConstructorArguments.Select(x => x.Value).Last();
                                            navType.ForeignKey = fk.ToString();


                                            navType.MetaData += navType.ForeignKey + "&con&";
                                        }

                                    }
                                    //if(!isNavProperty)
                                    //ProcessProperty(navType, true);

                                    customType.NavigationProperty.Add(navType);
                                }
                            }
                            else
                            {
                                if (property.PropertyType.BaseType.Name.ToLower() == "genericlookup")
                                {

                                    var res = GLMapperList.GenericLookUpList.FirstOrDefault(x => x.MappedName.ToLower() == property.Name.ToLower());
                                    if (res != null)
                                    {
                                        CustomType navType = new CustomType();
                                        navType.TableName = res.TableName;
                                        var gl = customObjectTypes.FirstOrDefault(x => x.TableName == res.TableName);
                                        navType.ObjectType = gl == null ? property.PropertyType : gl.ObjectType;
                                        navType.OriginalEntity = navType.ObjectType.Name;// property.Name;


                                        navType.MetaData += navType.TableName + "&con&";

                                        navType.MetaData += navType.OriginalEntity + "&con&";

                                        var foreignAtt = property.CustomAttributes.FirstOrDefault(x => x.AttributeType == typeof(ForeignKeyAttribute));

                                        if (foreignAtt != null)
                                        {
                                            var fk = foreignAtt.ConstructorArguments.Select(x => x.Value).FirstOrDefault();
                                            navType.ForeignKey = fk.ToString();

                                            navType.MetaData += navType.ForeignKey + "&con&";
                                        }
                                        else // check relationalkey
                                        {
                                            var frelationalAtt = property.CustomAttributes.FirstOrDefault(x => x.AttributeType.Name == "RelationalKeyAttribute");
                                            if (frelationalAtt != null)
                                            {
                                                var fk = frelationalAtt.ConstructorArguments.Select(x => x.Value).Last();
                                                navType.ForeignKey = fk.ToString();


                                                navType.MetaData += navType.ForeignKey + "&con&";
                                            }

                                        }

                                        customType.NavigationProperty.Add(navType);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                    }

                }
            }

        }

        private void ProcessAllNavigation(CustomType customType)
        {
            if (customType.TableName.ToLower() == "stylemeas")
            {

            }
            foreach (var nav in customType.NavigationProperty)
            {
                if (nav.TableName.ToLower() == "stylemeas")
                {

                }
                if (nav.Columns.Count == 0)
                    ProcessProperty(nav);

                foreach (var nav2 in nav.NavigationProperty)
                {
                    ProcessAllNavigation(nav2);
                }
            }
        }

        private void lstEntityResults_SelectionChangedExecute(object sender, SelectionChangedEventArgs e)
        {
            DataGrid entityResultGrid = lstEntityResults.dataGrid1;
            if (entityResultGrid.SelectedItem != null)
            {
                CustomType customType = entityResultGrid.SelectedItem as CustomType;
                if (customType != null)
                    objectCheckerControl.SetDataContext(customType, customObjectTypes);
            }
        }
    }
}
