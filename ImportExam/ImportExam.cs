using inRiver.Remoting.Extension.Interface;
using System.Collections.Generic;
using System.Linq;
using inRiver.Remoting.Extension;
using System.Xml.Linq;
using inRiver.Remoting.Objects;
using System.Globalization;
using inRiver.Remoting.Query;
using inRiver.Remoting.Log;
using System;

namespace ImportExam
{
    public class ImportExam : IInboundDataExtension
    {
        public inRiverContext Context { get; set; }

        public Dictionary<string, string> DefaultSettings => new Dictionary<string, string>();

        public string Delete(string value)
        {
            return "";
        }

        public string Update(string value)
        {
            return "";
        }

        public string Test()
        {
            return "ImportExam Test";
        }

        public string Add(string xmlString)
        {
            Context.Log(LogLevel.Debug, "[ImportExam]");
            if (string.IsNullOrEmpty(xmlString))
            {
                Context.Log(LogLevel.Debug, "[ImportExam] xmlString is null");
            }
            else
            {
                Context.Log(LogLevel.Debug, "[ImportExam] xmlString lenght is: " + xmlString.Length);
            }

            XDocument xdoc = XDocument.Parse(xmlString);
            XElement dataElement = this.GetDataElement(xdoc);

            Entity source = null;
            Entity target = null;

            // This loop traverses the entities in the xml
            foreach (XElement entityElement in dataElement.Elements())
            {
                string entityTypeString = entityElement.Name.LocalName;
                Entity entity = null;

                if (entityTypeString.Equals("Product"))
                {
                    string productNumber = this.GetProductNumberFromXML(entityElement);
                    entity = Context.ExtensionManager.DataService.GetEntityByUniqueValue("ProductNumber", productNumber, LoadLevel.Shallow);
                    bool newProduct = entity == null;


                    if (newProduct)
                    {
                        EntityType productEntityType = Context.ExtensionManager.ModelService.GetEntityType("Product");
                        entity = Entity.CreateEntity(productEntityType);
                        Field productNumberField = entity.GetField("ProductNumber");
                        productNumberField.Data = productNumber;
                    }

                    if (newProduct)
                    {
                        entity = Context.ExtensionManager.DataService.AddEntity(entity);
                    } else
                    {
                        entity = Context.ExtensionManager.DataService.UpdateEntity(entity);
                    }
                    source = entity;
                }
                else // Item
                {
                    string itemNumber = this.GetItemNumberFromXML(entityElement);
                    // TODO:
                    //See if the item exists, if not add it otherwise update
                    entity = Context.ExtensionManager.DataService.GetEntityByUniqueValue("ItemNumber", itemNumber, LoadLevel.Shallow);
                    Boolean newItem = entity == null;

                    if(newItem)
                    {
                        EntityType productEntityType = Context.ExtensionManager.ModelService.GetEntityType("Item");
                        entity = Entity.CreateEntity(productEntityType);
                        Field productNumberField = entity.GetField("ItemNumber");
                        productNumberField.Data = itemNumber;
                    }

                    if (newItem)
                    {
                        entity = Context.ExtensionManager.DataService.AddEntity(entity);
                    }
                    else
                    {
                        entity = Context.ExtensionManager.DataService.UpdateEntity(entity);
                    }
                    target = entity;
                }

                entity = this.HandleFieldsInEntity(entity, entityElement);

            }

            this.HandleLinks(source, target);

            return "Success!";
        }

        private Entity HandleFieldsInEntity(Entity entity, XElement entityElement)
        {

            foreach (XElement fieldElement in entityElement.Elements())
            {
                string fieldTypeId = fieldElement.Name.LocalName;

                Field field = null;
  
                field = entity.GetField(fieldTypeId);


                if (field != null)
                {
                    if (field.FieldType.DataType == DataType.String)
                    {
                        field.Data = StringParser(fieldElement);
                    }
                    else if (field.FieldType.DataType == DataType.LocaleString)
                    {
                        field.Data = LocaleStringParser(fieldElement);
                    }
                    else if (field.FieldType.DataType == DataType.CVL)
                    {
                        field.Data = CVLParser(fieldElement, field.FieldType.CVLId);
                    }
                    else if (field.FieldType.DataType == DataType.Boolean)
                    {
                        field.Data = BooleanParser(fieldElement);
                    }

                }
            }
            try
            {
                entity = Context.ExtensionManager.DataService.UpdateEntity(entity);
            }
            catch(Exception ex) {
                Context.Log(LogLevel.Error, "Could not save", ex);
            }
            return entity;
        
        }

        
        private void HandleLinks(Entity source, Entity target)
        { 
            Link productToItemLink = new Link();
            List<LinkType> linkTypes = Context.ExtensionManager.ModelService.GetLinkTypesForEntityType(source.EntityType.Id);
            LinkType productToItemLinkType = linkTypes.Find(l => l.TargetEntityTypeId == target.EntityType.Id);
            productToItemLink.LinkType = productToItemLinkType;
            productToItemLink.Source = source;
            productToItemLink.Target = target;

            if (!Context.ExtensionManager.DataService.LinkAlreadyExists(source.Id, target.Id, null, "ProductItem"))
            {
                Context.ExtensionManager.DataService.AddLinkLast(productToItemLink);
            }
        }

        #region ParseMethods

        private string StringParser(XElement fieldElement)
        {
            string data = string.Empty;

            return fieldElement.Value;
        }

        private LocaleString LocaleStringParser(XElement fieldElement)
        {
            LocaleString locale = new LocaleString(Context.ExtensionManager.UtilityService.GetAllLanguages());
            foreach (XElement stringLevel in fieldElement.Elements())
            {
                foreach (XElement valueLevel in stringLevel.Elements())
                {
                    XAttribute languageCulture = valueLevel.Attribute("language");
                    if (languageCulture != null)
                    {
                        CultureInfo cultureInfo = new CultureInfo(languageCulture.Value);
                        locale[cultureInfo] = valueLevel.Value;
                    }
                }
            }

            return locale; // Should be LocaleString
        }

        private string CVLParser(XElement fieldElement, string cvlName)
        {

            string data = string.Empty;

            var cvl = Context.ExtensionManager.ModelService.GetCVL(cvlName);
            if (cvl == null)
            {
                cvl = new CVL
                {
                    DataType = DataType.String,
                    Id = cvlName
                };

                cvl = Context.ExtensionManager.ModelService.AddCVL(cvl);
            }
            CVLValue cvlVal = Context.ExtensionManager.ModelService.GetCVLValueByKey(fieldElement.Value, cvl.Id);

            if (cvlVal == null)
            { 

                cvlVal = new CVLValue() { CVLId = cvl.Id, Key = fieldElement.Value };
                cvlVal = Context.ExtensionManager.ModelService.AddCVLValue(cvlVal);
            }

            if (cvl.DataType == DataType.String)
            {
                cvlVal.Value = fieldElement.Value;
            }
            else if (cvl.DataType == DataType.LocaleString)
            {
                LocaleString locale = new LocaleString(Context.ExtensionManager.UtilityService.GetAllLanguages());
                CultureInfo cultureInfo = new CultureInfo("en");
                locale[cultureInfo] = fieldElement.Value;
                cvlVal.Value = locale;
            }

            return fieldElement.Value;           
        }


        private bool BooleanParser(XElement fieldElement)
        {
            return fieldElement.Value == "1";
        }
        #endregion

        #region Private Help Methods

        private XElement GetDataElement(XDocument xdoc)
        {
            foreach (XElement element in xdoc.Descendants())
            {
                if (element.Name.LocalName == "data")
                {
                    return element;
                }
            }

            return null;
        }

        private string GetProductNumberFromXML(XElement entityElement)
        {
            return entityElement.Element("ProductNumber").Value;
        }

        private string GetItemNumberFromXML(XElement entityElement)
        {
            return entityElement.Element("ItemNumber").Value;
        }

        #endregion

        private void AddCvlValue(string cvlId, string newCVLValueKey, string newCVLValue)
        {
            CVL cvl = Context.ExtensionManager.ModelService.GetCVL(cvlId);
            bool newValue = false;
            CVLValue cvlVal = Context.ExtensionManager.ModelService.GetCVLValueByKey(newCVLValueKey, cvlId);

            if (cvlVal == null)
            {
                newValue = true;
                cvlVal = new CVLValue() { CVLId = cvlId, Key = newCVLValueKey };
            }


            if (cvl.DataType == DataType.String)
            {
                cvlVal.Value = newCVLValue;
            }
            else if (cvl.DataType == DataType.LocaleString)
            {
                LocaleString locale = new LocaleString(Context.ExtensionManager.UtilityService.GetAllLanguages());
                CultureInfo cultureInfo = new CultureInfo("en");
                locale[cultureInfo] = newCVLValue;
                locale[new CultureInfo("sv")] = newCVLValue + "_SV";
                cvlVal.Value = locale;
            }

            if (newValue)
            {
                Context.ExtensionManager.ModelService.AddCVLValue(cvlVal);
            }
            else
            {
                Context.ExtensionManager.ModelService.UpdateCVLValue(cvlVal);
            }
        }
    }

}
