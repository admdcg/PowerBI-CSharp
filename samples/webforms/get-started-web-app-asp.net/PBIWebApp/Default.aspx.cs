
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Threading.Tasks;
using System.Net.Http;
using System.Collections.Specialized;
using System.Web.Script.Serialization;
using System.Net;
using System.IO;

namespace PBIWebApp
{
 /* NOTE: This sample is to illustrate how to authenticate a Power BI web app. 
 * In a production application, you should provide appropriate exception handling and refactor authentication settings into 
 * a configuration. Authentication settings are hard-coded in the sample to make it easier to follow the flow of authentication. */
    public partial class _Default : Page
    {
        public AuthenticationResult authResult { get; set; }
        private const string datasetsUri = "https://api.powerbi.com/v1.0/myorg";

        protected void Page_Load(object sender, EventArgs e)
        {
            //Test for AuthenticationResult
            HttpCookie myCookie = Request.Cookies["PwBi_AccessToken"];
            

            //if (Session["authResult"] != null)
            if (!String.IsNullOrWhiteSpace(myCookie?.Value))
            {

                //Get the authentication result from the session
                //JavaScriptSerializer json = new JavaScriptSerializer();
                //authResult = (AuthenticationResult)json.Deserialize(myCookie.Value, typeof(AuthenticationResult));
                //authResult = (AuthenticationResult)Session["authResult"];
                

                //Show Power BI Panel
                PBIPanel.Visible = true;
                signinPanel.Visible = false;

                //Set user and toek from authentication result
                //userLabel.Text = authResult.UserInfo.DisplayableId;
                accessTokenTextbox.Text = myCookie.Value;                
            }
            else
            {
                PBIPanel.Visible = false;
            }
        }

        protected void signInButton_Click(object sender, EventArgs e)
        {
            //Create a query string
            //Create a sign-in NameValueCollection for query string
            var @params = new NameValueCollection
            {
                //Azure AD will return an authorization code. 
                //See the Redirect class to see how "code" is used to AcquireTokenByAuthorizationCode
                {"response_type", "code"},

                //Client ID is used by the application to identify themselves to the users that they are requesting permissions from. 
                //You get the client id when you register your Azure app.
                {"client_id", Properties.Settings.Default.ClientID},

                //Resource uri to the Power BI resource to be authorized
                {"resource", "https://analysis.windows.net/powerbi/api"},

                //After user authenticates, Azure AD will redirect back to the web app
                {"redirect_uri", Properties.Settings.Default.RedirectUrl}
            };
            
            //Create sign-in query string
            var queryString = HttpUtility.ParseQueryString(string.Empty);
            queryString.Add(@params);

            //Redirect authority
            //Authority Uri is an Azure resource that takes a client id to get an Access token
            string authorityUri = "https://login.windows.net/common/oauth2/authorize/";
            Response.Redirect(String.Format("{0}?{1}", authorityUri, queryString));       
        }        
        
        //The Get Tables operation returns a JSON list of Tables for the specified Dataset.
        //GET https://api.powerbi.com/v1.0/myorg/datasets/{dataset_id}/tables
        //Get Tables operation: https://msdn.microsoft.com/en-US/library/mt203556.aspx
        private static Tables GetTables(string datasetId, string accessToken)
        {
            Tables response = null;
            string responseContent;
            //In a production application, use more specific exception handling.
            try
            {
                //Create a GET web request to list all datasets
                HttpWebRequest request = DatasetRequest(String.Format("{0}/datasets/{1}/tables", datasetsUri, datasetId), "GET", accessToken);

                //Get HttpWebResponse from GET request
                responseContent = GetResponse(request);

                JavaScriptSerializer json = new JavaScriptSerializer();
                response = (Tables)json.Deserialize(responseContent, typeof(Tables));
            }
            catch (WebException ex)
            {
                using (var stream = ex.Response.GetResponseStream())
                {
                    using (var reader = new StreamReader(stream))
                    {
                        responseContent = reader.ReadToEnd();
                        JavaScriptSerializer json = new JavaScriptSerializer();
                        Error errResponse = (Error)json.Deserialize(responseContent, typeof(Error));
                        response = new Tables()
                        {
                            value = new table[]
                            {
                                new table { Name = errResponse?.error?.message}
                            }
                        };
                    }                    
                }
            }
            catch (Exception ex)
            {
                //In a production application, handle exception
            }

            return response;
        }
        
        protected void cmdLogout_Click(object sender, EventArgs e)
        {
            if (Request.Cookies["PwBi_AccessToken"] != null)
            {
                HttpCookie myCookie = new HttpCookie("PwBi_AccessToken");
                myCookie.Expires = DateTime.Now.AddDays(-1d);
                Response.Cookies.Add(myCookie);               
            }
            PBIPanel.Visible = false;
            signinPanel.Visible = true;
        }
        protected void cmdDatasetsButton_Click(object sender, EventArgs e)
        {
            var datasets = GetDatasets(accessTokenTextbox.Text);
            //var datasets = getDatasetsOld();
            if (datasets != null)
            {
                foreach (dataset ds in datasets.value)
                {
                    lstDatasets.Items.Add(new ListItem(ds.addRowsAPIEnabled ? String.Format("{0} (Permite añadir filas via API)", ds.Name) 
                                                                            : String.Format("{0} (No permite añadir filas via API)", ds.Name), ds.Id));
                }
            }
        }

        protected void cmdGetTable_Click(object sender, EventArgs e)
        {
            var tables = GetTables(lstDatasets.SelectedItem.Value, accessTokenTextbox.Text);
            if (tables != null)
            {
                foreach (table t in tables.value)
                {
                    txtTables.Text += String.Format("{0}\t", t.Name);
                }
            }           
        }

        //The Get Datasets operation returns a JSON list of all Dataset objects that includes a name and id.
        //GET https://api.powerbi.com/v1.0/myorg/datasets
        //Get Dataset operation: https://msdn.microsoft.com/en-US/library/mt203567.aspx
        private static Datasets GetDatasets(String accessToken)
        {            
            Datasets response = null;

            //In a production application, use more specific exception handling.
            try
            {
                //Create a GET web request to list all datasets
                HttpWebRequest request = DatasetRequest(String.Format("{0}/datasets", datasetsUri), "GET", accessToken);

                //Get HttpWebResponse from GET request
                string responseContent = GetResponse(request);

                JavaScriptSerializer json = new JavaScriptSerializer();
                response = (Datasets)json.Deserialize(responseContent, typeof(Datasets));
            }
            catch (Exception ex)
            {
                //In a production application, handle exception
            }
            return response;
        }

        private static string GetResponse(HttpWebRequest request)
        {   
            string response = string.Empty;
            using (HttpWebResponse httpResponse = request.GetResponse() as System.Net.HttpWebResponse)
            {
                using (StreamReader reader = new System.IO.StreamReader(httpResponse.GetResponseStream()))
                {
                    response = reader.ReadToEnd();
                }
            }                        
            return response;
        }

        private static HttpWebRequest DatasetRequest(string datasetsUri, string method, string accessToken)
        {
            HttpWebRequest request = System.Net.WebRequest.Create(datasetsUri) as System.Net.HttpWebRequest;
            //request.KeepAlive = true;
            request.Method = method;
            request.ContentLength = 0;
            request.ContentType = "application/json";
            request.Headers.Add("Authorization", String.Format("Bearer {0}", accessToken));
            return request;
        }
        
        protected Datasets getDatasetsOld()
        {
            string responseContent = string.Empty;            
            string datasetsUri = "https://api.powerbi.com/v1.0/myorg/datasets";                                              
            System.Net.WebRequest request = System.Net.WebRequest.Create(datasetsUri) as System.Net.HttpWebRequest;
            request.Method = "GET";
            request.ContentLength = 0;
            request.Headers.Add("Authorization", String.Format("Bearer {0}", authResult.AccessToken));            
            using (var response = request.GetResponse() as System.Net.HttpWebResponse)
            {            
                using (var reader = new System.IO.StreamReader(response.GetResponseStream()))
                {
                    responseContent = reader.ReadToEnd();             
                    JavaScriptSerializer json = new JavaScriptSerializer();
                    Datasets datasets = (Datasets)json.Deserialize(responseContent, typeof(Datasets));
                    return datasets;                    
                }
            }
        }
    }
}