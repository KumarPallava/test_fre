using System;
using System.Data;
using System.Collections;
using System.Collections.Specialized;
using AG.Data;
using AG.Library;
using AG.CustomerService.Common.Dox;


namespace AG.CustomerService.Common.DocumentFinishing
{
	/// <summary>
	/// Contains the data access functionality, filter functionality and general utility functions regarding attachments and attachment collections.
	/// </summary>
	public class AttachmentManager
	{
		#region Private Member Fields

		private IFilter _filter;
		private AttachmentCollection _availAttachments;
		private ArrayList _selectedReasons;
		private bool _defaultAttachSource;
		private bool _defaultAttachBlankForm;

		#endregion

		#region Constructors

		public AttachmentManager()
		{
			
		}

		public AttachmentManager(IFilter filter)
		{
			_filter = filter;
		}

		#endregion

		#region Public Properties

		public IFilter Filter
		{
			get { return _filter; }
			set { _filter = value; }
		}

		public AttachmentCollection AvailableAttachments
		{
			get { return _availAttachments; }
		}

		public ArrayList SelectedReasons
		{
			set { this._selectedReasons = value;}
			get { return this._selectedReasons; }
		}

		public bool DefaultAttachSource
		{
			get{return this._defaultAttachSource;}
		}

		public bool DefaultAttachBlankForm
		{
			get{return this._defaultAttachBlankForm;}

		}
		#endregion

		#region Public Methods

		/// <summary>
		/// Retrieves attachments using the default Filter object.
		/// </summary>
		/// <returns></returns>
		public AttachmentCollection GetAttachments()
		{
			if(this._filter==null)
			{
				throw new ArgumentNullException("Filter Property","The Filter property may not be null when calling this method");
			}
			
			return GetAttachments(_filter);
		}

		/// <summary>
		/// Retrieve attachments from the database for the specific work type.
		/// </summary>
		/// <param name="filter">An object implementing the IFilter to apply the retrieved attachments for a specified work type</param>
		/// <returns>Returns an instance of a populated attachment collection.</returns>
		public AttachmentCollection GetAttachments(IFilter filter)
		{
			AG.Data.DataManager dm = new AG.Data.DataManager();
			DataSet ds;
			ds = dm.GetDataSet("GetAttachments",Filter.WorkType.PadRight(10,Convert.ToChar(" ")));
			AttachmentCollection attachmentList = new AttachmentCollection();
			foreach(DataRow dr in ds.Tables[0].Rows)
			{
				Attachment atm = new Attachment(dr[8].ToString(),dr[6].ToString(),Convert.ToString(dr[0]),"",dr[1].ToString(),"",dr[2].ToString(),"","","");
				if(attachmentList.IndexOfKey(atm.AttachmentName,"AttachmentName")==-1)
					attachmentList.Add(atm);
			}
			attachmentList.TrimToSize();
			this._availAttachments = attachmentList;

			this.SetupDefaults(DocRecipient.Customer);
			return attachmentList;
		}

		/// <summary>
		/// Moves an attachment object from one attachment collection to another. 
		/// </summary>
		/// <param name="source">The attachment collection containing the object to move.</param>
		/// <param name="destination">The attachment collection the object will move to.</param>
		/// <param name="item">The attachment object instance to move.</param>
		public void CopyAndRemove(AttachmentCollection source,AttachmentCollection destination,Attachment item)
		{
			destination.Add(item);
			source.Remove(item);
		}

		private void SetupDefaults(DocRecipient recipient)
		{
			bool tempAttSource=false;
			bool tempAttForm=false;
			string tempSetting = "";
			string tempSettingForm = "";

			foreach(Reason req in GetUnmetFormsRequirements())
			{
				if(this._availAttachments.ContainsKey(req.Id))
				{
					//Check Source setting
					tempSetting = FormsRequirementsMgr.RequirementAttachSourceSetting(req.Id);
					if(tempSetting=="Y")
					{
						tempAttSource = true;
					}
					//else{tempAttSource =false;}

					//Check Blank Form setting
					tempSetting = FormsRequirementsMgr.RequirementAttachBlankFormSetting(req.Id);
					if(tempSetting == "Y")
					{ 
						tempAttForm = true;
						this._availAttachments.GetByKey(req.Id).IsSelected=true;
					}
					else if(tempSetting == "PO")
					{ //This is only applicable if going to the customer
						if(recipient == DocRecipient.Customer)
						{
							tempAttForm = true;
							this._availAttachments.GetByKey(req.Id).IsSelected=true;
						}
					}
					//else {tempAttForm = false;}
				}
				else  //We do not have a form in our list that matches the form needed in the unmet forms requirements
				{
					//Check Source setting
					if(tempAttForm==false) //We have to check if we have encountered a form we DO have, if we do - leave as is
					{
						tempSetting = FormsRequirementsMgr.RequirementAttachSourceSetting(req.Id);
						tempSettingForm = FormsRequirementsMgr.RequirementAttachBlankFormSetting(req.Id);
						if(tempSetting=="Y" && tempSettingForm=="Y")
						{
							tempAttSource = true;
							tempAttForm = false;
						}
						else if(tempSetting =="N" && tempSettingForm =="N")
						{
							tempAttSource = false;
							tempAttForm = false;
						}
						else if(tempSetting =="Y" && tempSettingForm=="N")
						{
							tempAttSource = true;
							tempAttForm = false;
						}
						else if(tempSetting =="N" && tempSettingForm =="Y")
						{
							tempAttSource = true;
							tempAttForm = false;
						}
						else if(tempSettingForm == "PO")
						{ //We don't have the form, therefore do not select the form.
							if (AG.AWD.AWDMgr.Instance.CurrentWorkObject.IsSmartPadWorkType)
								tempAttForm = true;
							else
								tempAttForm = false;
						}
						else 
						{
							tempAttForm = false;
							tempAttSource = true;
						}
					}
				}
			}
			//Now we determine the true defaults based on what things override each other
			if(tempAttSource==true&&tempAttForm==false)
			{
				this._defaultAttachBlankForm=false;
				this._defaultAttachSource=true;
				this._availAttachments.DeselectAll();
			}
			else if(tempAttSource==true&&tempAttForm==true)
			{
				this._defaultAttachBlankForm=true;
				this._defaultAttachSource=false;
				
			}
			else if(tempAttSource==false&&tempAttForm==true)
			{
				this._defaultAttachSource=false;
				this._defaultAttachBlankForm=true;
			}
			else
			{
				this._defaultAttachBlankForm=tempAttForm;
				this._defaultAttachSource=tempAttSource;
			}

		}

		public ArrayList GetUnmetFormsRequirements()
		{
			ArrayList values = new ArrayList();
			foreach (Reason item in this._selectedReasons)
			{
				if(item!=null)
				{
					if (item.Checked)
					{
						values.Add(item);
					}
				}
			}
			return values;

		}

		public void Refresh()
		{
			this.Refresh(DocRecipient.Customer);
		}

		public void Refresh(DocRecipient recipient)
		{
			this.SetupDefaults(recipient);
		}

		public bool IsAttachmentRequired(DocRecipient recipient)
		{
			bool tempAttSource=false;
			bool tempAttForm=false;
			string tempSetting = "";

			foreach(Reason req in GetUnmetFormsRequirements())
			{
				
				tempSetting = FormsRequirementsMgr.RequirementAttachSourceSetting(req.Id);
				if(tempSetting=="Y")
				{
					tempAttSource = true;
				}

				tempSetting = FormsRequirementsMgr.RequirementAttachBlankFormSetting(req.Id);
				if(tempSetting == "Y")
				{ 
					tempAttForm = true;
				}
				else if(tempSetting == "PO")
				{ //This is only applicable if going to the customer
					if(recipient == DocRecipient.Customer)
					{
						tempAttForm = true;
						
					}
				}
				
			}

			if(tempAttSource&&tempAttForm)
			{
				return true;
			}
			else if(!tempAttSource && tempAttForm)
			{
				return true;
			}
			else if(tempAttSource && !tempAttForm)
			{
				return true;
			}
			else if (!tempAttSource && !tempAttForm)
			{
				return false;
			}
			else 
			{
				return false;
			}
	}
		#endregion
	}
}
