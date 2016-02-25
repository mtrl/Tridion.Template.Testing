using System;
using System.Configuration;
using System.ServiceModel;
using System.Net;
using System.Diagnostics;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Collections;
//using System.Collections.Specialized;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Tridion.ContentManager.CoreService.Client;
using Tridion.ContentManager.CoreService.Client.Security;

namespace Tridion.Template.Testing
{
    [TestClass]
    public class TemplatingTests
    {
        private SessionAwareCoreServiceClient _client;
        private TestContext testContextInstance;
        private Stopwatch stopwatch;

        /// <summary>
        /// Get a Core Service client object.
        /// This is lifted from Alex Klock @ http://codedweapon.com/2013/03/changing-components-schemas-with-core-service/
        /// </summary>
        public SessionAwareCoreServiceClient Client
        {
            get
            {
                if (_client == null)
                {
                    string endpointName = ConfigurationManager.AppSettings["CoreServiceEndpoint"].ToString();
                    if (String.IsNullOrEmpty(endpointName))
                    {
                        throw new ConfigurationErrorsException("CoreServiceEndpoint missing from appSettings");
                    }

                    _client = new SessionAwareCoreServiceClient(endpointName);

                    string username = TestContext.Properties["TridionUsername"].ToString();
                    string password = TestContext.Properties["TridionPassword"].ToString();


                    if (!String.IsNullOrEmpty(username) && !String.IsNullOrEmpty(password))
                    {
                        _client.ClientCredentials.Windows.ClientCredential = new NetworkCredential(username, password);
                    }
                }
                return _client;
            }
        }

        public TemplatingTests()
        {
            // Let's have some metrics for the tests
            stopwatch = new Stopwatch();
        }

        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        [TestMethod]
        public void TestNoExceptionOnRenderingComponents()
        {
            string componentsOrgItemId = TestContext.Properties["ComponentsOrgItemId"].ToString();

            if (!String.IsNullOrEmpty(componentsOrgItemId))
            {
                OrganizationalItemItemsFilterData filter = new OrganizationalItemItemsFilterData();
                filter.ItemTypes = new ItemType[] { ItemType.Component };
                filter.Recursive = true;

                foreach (XElement element in Client.GetListXml(componentsOrgItemId, filter).Nodes())
                {
                    ComponentData component = Client.Read(element.Attribute("ID").Value, null) as ComponentData;
                    this.LogMessage(String.Format("Running template regression tests for component \"{0}\" ({1})", component.Title, component.Id), true);

                    foreach (XElement componentTemplateElement in GetUsingComponentTemplates(component))
                    {
                        ComponentTemplateData componentTemplate = Client.Read(componentTemplateElement.Attribute("ID").Value, null) as ComponentTemplateData;
                        // Render the component with the component template
                        this.AssertItemRendersWithoutException(component, componentTemplate);
                    }

                    this.LogMessage("PASSED! :)");
                }
            }
        }

        [TestMethod]
        public void TestNoExceptionOnRenderingPages()
        {
            string pagesOrgItemId = TestContext.Properties["PagesOrgItemId"].ToString();
            if (!String.IsNullOrEmpty(pagesOrgItemId))
            {
                OrganizationalItemItemsFilterData filter = new OrganizationalItemItemsFilterData();
                filter.ItemTypes = new ItemType[] { ItemType.Page };
                filter.Recursive = true;

                foreach (XElement element in Client.GetListXml(pagesOrgItemId, filter).Nodes())
                {
                    PageData page = Client.Read(element.Attribute("ID").Value, null) as PageData;
                    PageTemplateData pageTemplate = Client.Read(page.PageTemplate.IdRef, null) as PageTemplateData;
                    this.LogMessage(String.Format("Running template regression tests for page \"{0}\" ({1})", page.Title, page.Id), true);

                    // Render the page with the page template
                    this.AssertItemRendersWithoutException(page, pageTemplate);

                    this.LogMessage("PASSED! :)");
                }
            }
        }

        /// <summary>
        /// Get the schema used by the specified component
        /// </summary>
        /// <param name="componentId">The ID of the component</param>
        /// <returns>The schema used by the component</returns>
        private SchemaData GetComponentSchema(string componentId) {
            return Client.Read(componentId, null) as SchemaData;
        }

        /// <summary>
        /// Gets a list of all component templates used by this schema
        /// </summary>
        /// <param name="schemaId">The schema ID</param>
        /// <returns><![CDATA[An IEnumerabe<XNode> list of component templates]]></returns>
        private IEnumerable<XNode> GetUsingComponentTemplates(ComponentData component)
        {
            SchemaData componentSchema = GetComponentSchema(component.Schema.IdRef);
            UsingItemsFilterData usingFilter = new UsingItemsFilterData();
            usingFilter.ItemTypes = new[] { ItemType.ComponentTemplate };
            usingFilter.IncludedVersions = VersionCondition.OnlyLatestVersions;
            LinkToRepositoryData contextRepository = new LinkToRepositoryData()
            {
                IdRef = TestContext.Properties["TemplatePublicationId"].ToString()
            };
            //usingFilter.InRepository = contextRepository;

            return Client.GetListXml(componentSchema.Id, usingFilter).Nodes();
        }

        private void AssertItemRendersWithoutException(IdentifiableObjectData item, IdentifiableObjectData template)
        {
            RenderedItemData renderedItem = new RenderedItemData();
            try
            {
                string testPublicationId = TestContext.Properties["TestPublicationId"].ToString();
                string localisedItemId = Client.GetTcmUri(item.Id, testPublicationId, null);

                string localisedTemplateId = Client.GetTcmUri(template.Id, TestContext.Properties["TestPublicationId"].ToString(), null);

                RenderInstructionData renderInstruction = new RenderInstructionData();
                renderInstruction.RenderMode = RenderMode.PreviewStatic;
            
                PublishInstructionData previewInstruction = new PublishInstructionData();
                ResolveInstructionData resolveInstruction = new ResolveInstructionData();
            
                previewInstruction.RenderInstruction = renderInstruction;
                previewInstruction.ResolveInstruction = resolveInstruction;

                stopwatch.Restart();
                renderedItem = Client.RenderItem(localisedItemId, localisedTemplateId, previewInstruction, "tcm:0-1-65537");
                long timeToRender = stopwatch.ElapsedMilliseconds;
                string renderedContent = System.Text.Encoding.Default.GetString(renderedItem.Content);
                this.LogMessage(String.Format("Rendering with template \"{0}\" ({1}) succeeded and took {2} milliseconds.", template.Title, localisedTemplateId, timeToRender));

                Assert.IsNotNull(renderedContent);
            }
            catch (System.ServiceModel.FaultException ex)
            {
                string failMessage = String.Format("TEST FAILED! Failed when rendering item {0} ({1}) with template {2} ({3}) {4}. Exception: {5}", item.Id, item.Title, template.Id, template.Title, Environment.NewLine + Environment.NewLine, ex.Message);
                this.LogMessage(failMessage);
                Assert.Fail(failMessage);
            }
        }

        /// <summary>
        /// Logs messages to console for Jenkins and testcontext for commandline/visual studio
        /// </summary>
        /// <param name="message">The message you want to log</param>
        private void LogMessage(string message, bool header = false)
        {
            if (header)
            {
                this.TestContext.WriteLine("----------------------");
            }
            this.TestContext.WriteLine(message);
            if (header)
            {
                this.TestContext.WriteLine("----------------------");
            }
        }
        
    }
}
