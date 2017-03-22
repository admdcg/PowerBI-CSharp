using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace PBIWebApp
{
 /* NOTE: This sample is to illustrate how to authenticate a Power BI web app. 
 * In a production application, you should provide appropriate exception handling and refactor authentication settings into 
 * a configuration. Authentication settings are hard-coded in the sample to make it easier to follow the flow of authentication. */

    public partial class Redirect : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            //Redirect uri must match the redirect_uri used when requesting Authorization code.
            string redirectUri = Properties.Settings.Default.RedirectUrl;
            string authorityUri = "https://login.windows.net/common/oauth2/authorize/";
           
            // Get the auth code
            string code = Request.Params.GetValues(0)[0];
            
            // Get auth token from auth code       
            TokenCache TC = new TokenCache();

            AuthenticationContext AC = new AuthenticationContext(authorityUri, TC);

            ClientCredential cc = new ClientCredential
                (Properties.Settings.Default.ClientID,
                Properties.Settings.Default.ClientSecretKey);

            //AuthenticationResult AR = AC.AcquireTokenByAuthorizationCode(code, new Uri(redirectUri), cc);
            var task = AC.AcquireTokenByAuthorizationCodeAsync(code, new Uri(redirectUri), cc);

            //Set Session "authResult" index string to the AuthenticationResult
            AuthenticationResult AR = task.Result;            
            
//            string jsonAR = new JavaScriptSerializer().Serialize(AR);
            var cookie = new HttpCookie("PwBi_AccessToken", AR.AccessToken)
            {
                Expires = DateTime.Now.AddDays(1d)
            };
            Response.Cookies.Add(cookie);            
            //Session["authResult"] = AR;

            //Redirect back to Default.aspx
            Response.Redirect("/Default.aspx");
        }
    }
}