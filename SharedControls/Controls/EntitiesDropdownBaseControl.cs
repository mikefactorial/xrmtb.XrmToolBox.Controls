﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Xrm.Sdk.Metadata;

namespace xrmtb.XrmToolBox.Controls
{
    public partial class EntitiesDropdownBaseControl : BoundDropdownControl
    {
        public EntitiesDropdownBaseControl()
        {
            InitializeComponent();
        }

        #region Public Properties
        /// <summary>
        /// Defines which Entity types should be loaded on retrieve.
        /// </summary>
        [Category("XrmToolBox")]
        [DisplayName("Solution Filter")]
        [Description("Specifies a Solution Unique Name filter to be used when retrieving Entities.")]
        public string SolutionFilter { get; set; }

        /// <summary>
        /// The currently selected EntityMetadata object in the ListView
        /// </summary>
        [DisplayName("Selected Entity")]
        [Description("The Entity that is currently selected in the Dropdown.")]
        [Category("XrmToolBox")]
        [Browsable(false)]
        public EntityMetadata SelectedEntity
        {
            get => (SelectedItem as ListDisplayItem)?.Object as EntityMetadata;
        }

        /// <summary>
        /// List of all loaded EntityMetadata objects for the current connection
        /// </summary>
        [Description("List of all Entities that have been loaded into the Dropdown.")]
        [Category("XrmToolBox")]
        [Browsable(false)]
        public List<EntityMetadata> AllEntities {
            get => _allItems.Select(i => i.Object as EntityMetadata).ToList();
        }

        /// <summary>
        /// Reference to all Entities as a bindable list
        /// </summary>
        [Category("XrmToolBox")]
        [Description("Reference to all Entities as a bindable list")]
        [Browsable(false)]
        public List<ListDisplayItem> AllEntitiesBindable { get => DataSource as List<ListDisplayItem>; }
        #endregion



        #region IXrmToolBoxControl methods

        /// <summary>
        /// Private method that will rethrow an Exception if specified in the parameter.  
        /// This is meant to allow for external programmatic calls to load vs those from the built in controls
        /// </summary>
        /// <param name="throwException">Flag indicating whether to rethrow a captured exception</param>
        private void LoadData(bool throwException)
        {
            OnBeginLoadData();

            if (Service == null)
            {
                var ex = new InvalidOperationException("The Service reference must be set before loading the Entities list");

                // raise the error event and if set, throw error
                OnNotificationMessage(ex.Message, MessageLevel.Exception, ex);

                if (throwException)
                {
                    throw ex;
                }
                return;
            }

            ClearData();

            try
            {
                OnProgressChanged(0, "Begin loading Entities from CRM");

                // retrieve all entities and bind to the combo
                // var entities = CrmActions.RetrieveAllEntities(Service);
                var worker = new BackgroundWorker();

                worker.DoWork += (w, e) => {

                    var entities = new List<EntityMetadata>();
                    if (SolutionFilter != null)
                    {
                        entities = CrmActions.RetrieveEntitiesForSolution(Service, SolutionFilter);
                    }
                    else
                    {
                        entities = CrmActions.RetrieveAllEntities(Service);
                    }

                    e.Result = entities;
                };

                worker.RunWorkerCompleted += (s, e) =>
                {
                    var entities = e.Result as List<EntityMetadata>;

                    LoadComboItems();

                    OnProgressChanged(100, "Loading Entities from CRM Complete!");

                    base.LoadData();
                };

                // kick off the worker thread!
                worker.RunWorkerAsync();
            }
            catch (System.ServiceModel.FaultException ex)
            {
                OnNotificationMessage($"An error occured attetmpting to load the list of Entities", MessageLevel.Exception, ex);

                if (throwException)
                {
                    throw ex;
                }
            }
        }
        #endregion

        #region Private helper methods
        /// <summary>
        /// Load the list of Entities into the Combo control
        /// </summary>
        private void LoadComboItems()
        {
            var items = from ent in AllEntities
                        select new ListDisplayItem(
                            ent.SchemaName,
                            CrmActions.GetLocalizedLabel(ent.DisplayName, ent.SchemaName, LanguageCode),
                            CrmActions.GetLocalizedLabel(ent.Description, null, LanguageCode),
                            ent);

            LoadData(items.ToList());
        }

        #endregion

        #region Control event handlers

        private void ComboEntities_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (SelectedItem is ListDisplayItem item)
            {
                // check to see if we want to raise the change event
                var ent = item.Object as EntityMetadata;
                if (ent.LogicalName == SelectedEntity?.LogicalName)
                {
                    return;
                }
            }

            OnSelectedItemChanged(new EventArgs());
        }
        
        #endregion
    }
}
