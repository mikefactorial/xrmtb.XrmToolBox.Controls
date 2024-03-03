﻿using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using xrmtb.XrmToolBox.Controls.Helper;

namespace xrmtb.XrmToolBox.Controls.Controls
{
    public partial class CDSDataTextBox : TextBox
    {
        #region Private properties
        private string displayFormat = string.Empty;
        private bool clickable = false;
        private string logicalName = null;
        private Guid id = Guid.Empty;
        private EntityWrapper entity;
        private IOrganizationService organizationService;
        private Font font;
        #endregion

        #region Public Constructors

        public CDSDataTextBox()
        {
            InitializeComponent();
            font = Font;
            base.ReadOnly = true;
            BackColor = SystemColors.Window;
            Click += HandleClick;
        }

        #endregion Public Constructors

        #region Public Properties

        [Category("Data")]
        [DisplayName("Record LogicalName")]
        [Description("LogicalName if the entity type")]
        public string LogicalName
        {
            get
            {
                return logicalName;
            }
            set
            {
                if (value?.Equals(logicalName) == true)
                {
                    return;
                }
                logicalName = value;
                id = Guid.Empty;
                entity = null;
                Refresh();
            }
        }

        [DefaultValue("00000000-0000-0000-0000-000000000000")]
        [Category("Data")]
        [DisplayName("Record Id")]
        [Description("Id of the record. LogicalName must be set before setting the Id.")]
        public Guid Id
        {
            get
            {
                return id;
            }
            set
            {
                if (value.Equals(id))
                {
                    return;
                }
                id = string.IsNullOrWhiteSpace(logicalName) ? id = Guid.Empty : value;
                LoadRecord();
                Refresh();
            }
        }

        [Category("Data")]
        [DisplayName("Display Format")]
        [Description("Single attribute from datasource to display for items, or use {{attributename}} syntax freely.")]
        public string DisplayFormat
        {
            get { return displayFormat; }
            set
            {
                if (value?.Equals(displayFormat) == true)
                {
                    return;
                }
                displayFormat = value;
                Refresh();
            }
        }

        [DefaultValue(false)]
        [Category("Behavior")]
        [DisplayName("Record Clickable")]
        [Description("Displays the record text as a clickable text and fires RecordClick event when clicked")]
        public bool Clickable
        {
            get
            {
                return clickable;
            }
            set
            {
                if (clickable.Equals(value))
                {
                    return;
                }
                clickable = value;
                if (clickable)
                {
                    ForeColor = SystemColors.HotTrack;
                    Font = new Font(font, Font.Style | FontStyle.Underline);
                    Cursor = Cursors.Hand;
                }
                else
                {
                    ForeColor = SystemColors.ControlText;
                    Font = font;
                    Cursor = Cursors.IBeam;
                }
            }
        }

        [ReadOnly(true)]
        public new bool ReadOnly { get; set; } = true;

        [Browsable(false)]
        public IOrganizationService OrganizationService
        {
            get { return organizationService; }
            set
            {
                if (value == organizationService)
                {
                    return;
                }
                Entity = null;
                organizationService = value;
                LoadRecord();
                Refresh();
            }
        }

        [Browsable(false)]
        public EntityReference EntityReference
        {
            get
            {
                var result = entity?.Entity?.ToEntityReference();
                if (result == null)
                {
                    return null;
                }
                result.Name = EntityWrapper.EntityToString(entity?.Entity, organizationService);
                return result;
            }
            set
            {
                if (value?.LogicalName == logicalName && value?.Id.Equals(Id) == true)
                {
                    return;
                }
                LogicalName = value?.LogicalName;
                Id = value?.Id ?? Guid.Empty;
                Refresh();
            }
        }

        [Browsable(false)]
        public Entity Entity
        {
            get
            {
                return entity?.Entity;
            }
            set
            {
                if (entity?.Entity?.Id.Equals(value?.Id) == true)
                {
                    return;
                }
                entity = value != null ? new EntityWrapper(value, displayFormat, organizationService) : null;
                logicalName = value?.LogicalName;
                id = value?.Id ?? Guid.Empty;
                Refresh();
            }
        }

        #endregion Public Properties

        #region Published events

        [Category("CRM")]
        public event CDSRecordEventHandler RecordClick;

        #endregion Published Events

        #region Private Methods

        private void HandleClick(object sender, EventArgs e)
        {
            if (!clickable)
            {
                return;
            }
            new CDSRecordEventArgs(entity?.Entity, null).OnRecordEvent(this, RecordClick);
        }

        private void LoadRecord()
        {
            if (organizationService != null && !string.IsNullOrWhiteSpace(logicalName) && !Guid.Empty.Equals(Id))
            {
                var record = organizationService.Retrieve(logicalName, Id, new ColumnSet(true));
                entity = new EntityWrapper(record, displayFormat, organizationService);
            }
            else
            {
                entity = null;
            }
        }

        #endregion Private Methods

        #region Public Methods

        public override void Refresh()
        {
            if (entity != null && !entity.Format.Equals(displayFormat))
            {
                entity.Format = displayFormat;
            }
            Text = entity?.ToString();
            base.Refresh();
        }

        #endregion Public Methods
    }
}
