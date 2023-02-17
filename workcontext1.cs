using System;
using System.Collections.Specialized;
using System.Collections;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;
using AG.Data;
using AG.Library;
using AG.Library.Common;
using AG.Library.BaseClass;
using System.Diagnostics;
using AG.Library.Exceptions;
using AG.Library.ExceptionManagement;
using AG.Library.Persistence;
using SWB.Library;
using SWB.Library.Exceptions;
using AG.CustomerService.Common.Transactions;
using AG.AWD;
namespace AG.CustomerService.Common
{
  
	/// <summary>
	/// Event data that describes the WorkDataChanged event
	/// </summary>

	public class PersistEventArgs : System.EventArgs
	{
		public AG.Library.Persistence.PersistenceList _persistList;
		public PersistEventArgs(PersistenceList list)
		{
			_persistList = list;
		}
		public PersistenceList PersistList
		{
			get{ return _persistList; }
			set{ _persistList = value; }
		}
	}

	/// <summary>
	/// Work context provides generalized data on the current work being done. IE, what
	/// work is being done on which policy and who's doing it. The class also provides
	/// some general functionality for operating on the current work object as well as
	/// events for serialization and deserialization of the work object.
	/// </summary>
	public sealed class WorkContext
	{
		#region declares
		#region events
		public delegate void PersistEventHandler(object sender, PersistEventArgs e);   
    
		public static event EventHandler EnabledChanged;
		public static event EventHandler ProcessEnabledChanged;
		public static event PersistEventHandler Serialize;
		public static event PersistEventHandler Deserialize;
    
		#endregion

		static string _mailCd;
		static string _deskCd;   
		static string _userId;
		//static string _userName;
		static string _key;
		static bool _enabled = false;
		static bool _workComplete = false;
		static bool _workLoaded = false;
		static bool _immediateDone = false;
		static bool _skipMainFrameEdits = false;
		static bool _processEnabled = true;
		static string _formReqOtherName = "";
		static AG.Data.DataManager dm = null;
		static bool _ownerDeleted = false;
		static bool _retryGetWork = false;
		static bool _lookupWork = false;
		static Hashtable _cache = new Hashtable();
		#endregion
		#region ctors
		//make this private so you can't instantiate a new object
		private WorkContext(){}
		#endregion
		#region Utility Methods
		/// <summary>
		/// go to mainframe and check security file for desk code and mail code
		/// validate the login id to make sure that they user in proper role
		/// </summary>
		public static bool ValidateUser(out string retMsg) 
		{
			string retDoc;
			retMsg = "";
			try 
			{
                _userId = AGUtil.UserNameLAN();

            }
			catch(Exception ex) 
			{
				retMsg = "SWB could not validate user.  " + "Error obtaining Windows identity:  " + ex.Message;
				return false;
			}
      
			try
			{
				retDoc = DataManager.GetXMLData("GetUserInfo", new object[] {_userId, "G", _userId});
			}
			catch(Exception ex)
			{
				retMsg = "SWB could not validate user.  " + "Error calling Gateway query ZZ01P00 (user security info.)  " + ex.Message;
				return false;
			}
			try 
			{
				XmlDocument doc=new XmlDocument();
				doc.LoadXml(retDoc);
				_mailCd=doc.SelectSingleNode("/GetUserInfo/GetUserInfo_Table/MAIL-CODE").InnerText.Trim();
				_deskCd=doc.SelectSingleNode("/GetUserInfo/GetUserInfo_Table/DESK_CODE").InnerText.Trim();
			}
			catch(Exception ex)  
			{
				retMsg = "SWB could not validate user.  " + "Error processing user security information obtained from mainframe.  " + ex.Message;
				return false;
			}

			return true;
		}

		public static void PersistWork(PersistenceList list)
		{
			if(Serialize != null)
			{
				PersistEventArgs e = new PersistEventArgs(list);
				//Fire the serialize events so all registered
				//components can add relevant data to the persitence list
				Serialize(null,e);
				if(e.PersistList.Count > 0)
				{
					PersistManager.Serialize(list);
				}
			}

		}

		public static PersistenceList HydrateWork()
		{
      
			PersistenceList list = PersistManager.Deserialize(UniqueWorkKey,true);
			if(Deserialize != null && list != null)
			{
				PersistEventArgs e = new PersistEventArgs(list);
				Deserialize(null,e);
			}
			return list;
		}

		public static bool IsRestrictedLocalOffice(string district, string agency)
		{
			//-- Pattern for restricted local offices
			string pattern = @"(B[0-9][0-9][0-9])|(EL99)|(G[0-9][0-9][0-9])|(Q[A-Z0-9][A-Z0-9][A-Z0-9])|(X[A-Z0-9][A-Z0-9][A-Z0-9])|(HO[A-Z0-9][A-Z0-9])|(WS[A-Z0-9][A-Z0-9])";
			return Regex.IsMatch(district,pattern);
		}

		public static void TransferPolicy(string newDistrict, string newAgency, string newFamilyGroup)
		{
			StringCollection parms = new StringCollection();
			parms.Add("W");//Function Code - 'W'rite transaction
			parms.Add("01");//Number of Transactions
			parms.Add(PolicyDataMgr.Instance.PolicyDetail.CompanyCode); //Company code
			parms.Add(PolicyDataMgr.Instance.PolicyDetail.PolicyNo);//Policy Number
			parms.Add("ZZZZ");//Check digits
			parms.Add("058");//Transaction code
			parms.Add("C");//System code
			parms.Add(WorkContext.DeskCode);//Desk code
			parms.Add(PolicyDataMgr.Instance.PolicyDetail.PaidToDate);//Paid to date
			parms.Add(newDistrict);//New District
			parms.Add(newAgency);//New Agency
			parms.Add("  ");//New Cycle
			parms.Add(newFamilyGroup); //New family number
			parms.Add("  "); //New number prems
			parms.Add(PolicyDataMgr.Instance.PolicyDetail.District); //Old district
			parms.Add(PolicyDataMgr.Instance.PolicyDetail.Agency); //Old agency

			DataManager.GetXMLData("TransferPolicy", parms);

		}

		public static ArrayList ChangeOwnerSSN(Array socialSecurityNos)
		{
			ArrayList errs = new ArrayList();

			foreach(SSN ssn in socialSecurityNos) 
			{
				if( ssn == null )
					continue;
				if(ssn.SocialSecurityNo.Length > 0) 
				{
					string[] parms = new string[66];
          
					parms[0] = ssn.SocialSecurityNo;
					parms[1] = WorkContext.DeskCode;
					parms[2] = "";
					parms[3] = "S";
					parms[4] = "01";
					parms[5] = PolicyDataMgr.Instance.PolicyDetail.CompanyCode;
					parms[6] = PolicyDataMgr.Instance.PolicyDetail.PolicyNo;
					parms[7] = "ZZZZ";
					parms[8] = ssn.Role;
					parms[9] = PolicyDataMgr.Instance.PolicyDetail.IsVantage ? "Y" : "N";
					parms[10] = ssn.Certified ? "Y" : "N";
          
					for(int ctr = 11;ctr < 65;ctr++)
						parms[ctr] = "";

					if (PolicyDataMgr.PolicyInfo.IsVantage)
						parms[65] = ((AG.CustomerService.PolicyRole.Role)PolicyDataMgr.Instance.PolicyDetail.Roles.GetRoleByIdentifier(ssn.Role)).DirID.ToString();
					else
						parms[65] = " ";

					try 
					{
						DataManager.Update("UpdateSSN",parms);
            
					}
					catch(AGUpdateException ex) 
					{
						errs.Add(ex);      
					}
				}
			}      
			return errs;
		}

		/// <summary>
		/// Performs a file retrieval. Action and save will change depending on context called.
		/// </summary>
		/// <param name="companyCode"></param>
		/// <param name="policyNumber"></param>
		/// <param name="checkDigits"></param>
		/// <param name="action"></param>
		/// <param name="mailCode"></param>
		/// <param name="deskCode"></param>
		/// <param name="save"></param>
		public static SWB.Library.FileRetrievalReturnType DoFileRetrieval(string companyCode, string policyNumber, string checkDigits, 
			string action, string mailCode, string deskCode, bool save,out string errorMessage )  
		{
			SWB.Library.FileRetrievalReturnType ret = DoFileRetrievalForce (companyCode, policyNumber, checkDigits, action, mailCode, deskCode, false, save, out errorMessage);
			return ret;
		}
				
		private static SWB.Library.FileRetrievalReturnType DoFileRetrievalForce(string companyCode, string policyNumber, string checkDigits, 
			string action, string mailCode, string deskCode, bool force, bool save, out string errorMessage )  
		{

      
			SWB.Library.FileRetrievalReturnType _ret = SWB.Library.FileRetrievalReturnType.NA;
			errorMessage = "";
			string[] parms = new string[10] 
				{
					companyCode,
					policyNumber,
					checkDigits,
					" ",
					action,
					mailCode,
					deskCode,
					"  ",
					force ? "Y":" ",
					save ? "Y":" "
				};

			try 
			{
				DataManager.Update("SendFileRetrieval", parms);
			}

			catch(FileRetrievalException ex) 
			{
				if (ex.Message.IndexOf("UNABLE TO VALIDATE") > -1)
				{
					_ret = DoFileRetrievalForce(companyCode, policyNumber, checkDigits, action, mailCode, deskCode, true, save, out errorMessage);
				}
				else
				{
					_ret = ex.ExceptionType;
					errorMessage = ex.Message;
				}
				Trace.WriteLine(string.Format("File Retrieval Result: {0} {1}",_ret.ToString(),ex.DisplayMessage));
			}
			return _ret;
		}

		public static bool EnableExtendedLetterComments()
		{
			bool _enableRejectLetterDetails = false;
			AG.Data.DataManager dm = new AG.Data.DataManager();
			AWDNode work = AWDMgr.Instance.CurrentWorkObject;
			string worktype = work != null ? work.WorkType : "";
			System.Data.DataSet ds  = dm.GetDataSet("GetAppConfigByContext",worktype);
			System.Data.DataRowCollection rows = ds.Tables[0].Rows;
			if( rows.Count > 0 )
			{
				foreach( System.Data.DataRow row in rows )
				{
					if( row["CONFIG_NAME"].ToString() == "Extended Ltr Cmnts" )
					{
						_enableRejectLetterDetails = row["CONFIG_VAL"].ToString() == "1";
						break;
					}
				}
			}
			return _enableRejectLetterDetails;
		}
    
		#endregion
		#region Properties
		public static Hashtable ObjectCache
		{
			get{ return _cache; }
		}
		public static string UserId 
		{
			get {return _userId;}
		}

		public static string MailCode  
		{
			get {return _mailCd;}
		}

		public static string DeskCode  
		{
			get {return _deskCd;}
		}

		public static string Key
		{
			get{ return _key;}
			set{ _key = value; }
		}

		public static string UniqueWorkKey
		{
			get
			{
				return WorkContext._key+PolicyDataMgr.Instance.PolicyDetail.PolicyNo + PolicyDataMgr.Instance.PolicyDetail.CompanyCode ;
			}
		}

		public static bool Enabled
		{
			get{ return _enabled; }
			set
			{ 
				if( value != _enabled) 
				{
					_enabled = value;
					if( EnabledChanged != null )
						EnabledChanged(null,new System.EventArgs());
				}
			}
		}

		public static bool ProcessEnabled
		{
			get {return _processEnabled;}
			set
			{
				if (value != _processEnabled)
				{
					_processEnabled = value;
					if (ProcessEnabledChanged != null)
						ProcessEnabledChanged(null,System.EventArgs.Empty);
				}						
			}
		}

		public static bool WorkComplete
		{
			get{ return _workComplete; }
			set{ _workComplete = value; }
		}

		public static bool WorkLoaded
		{
			get{ return _workLoaded; }
			set{_workLoaded = value; }
		}

		public static bool ImmediateDone
		{
			get{ return _immediateDone; }
			set{_immediateDone = value; }
		}

		public static bool LookUpWork
		{
			get{ return _lookupWork; }
			set{_lookupWork = value; }
		}

		public static bool RetryGetWork
		{
			get{ return _retryGetWork; }
			set{_retryGetWork = value; }
		}

		public static bool SkipMainFrameEdits				//Initialized in EditDriver.cs to false
		{
			get{ return _skipMainFrameEdits; }
			set{_skipMainFrameEdits = value; }
		}

		public static string FormReqOtherName
		{
			get{ return _formReqOtherName; }
			set{ _formReqOtherName = value; }
		}
	
		public static bool OwnerDeleted
		{
			get{ return _ownerDeleted; }
			set{ _ownerDeleted = value; }
		}

		public static void Reset()
		{      
			_key = "";
			_enabled = false;
			_workComplete = false;
			_workLoaded = false;
			_processEnabled = true;
			_cache.Clear();
		}

		public static AG.Data.DataManager DataManager
		{
			get 
			{
				if(WorkContext.dm == null)
					dm = new AG.Data.DataManager();

				return dm;
			}
		}
  
		#endregion
	}

}
