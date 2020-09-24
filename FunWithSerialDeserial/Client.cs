using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Text;
using System.Linq;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("XertClientNUnitTest")]

namespace XertClient
{
	public class Client : IXertClient
	{
		internal Client(HttpMessageHandler handler)
		{
			_Client = new HttpClient(handler);
		}
		public Client()
		{
			_Client = new HttpClient();
		}

		readonly HttpClient _Client;
		internal BarrierToken _Token;

		/// <summary>
		/// Gets an access token, available to registered users. The function should throw on any login problem.
		/// The curl message is:
		/// curl -u xert_public:xert_public -POST "https://www.xertonline.com/oauth/token" -d 'grant_type=refresh_token' -d 
		/// 'refresh_token=1badfdee0f72b847dc91d1baf9e5c095c774c14a'
		/// Exceptions in deserialize are handled and used to fill in error information of the BarrierToken, with the risk of loosing the
		/// stack trace, but these should be obvious errors but this is worth reconsidering 8/27/2020. I think it may be more useful to 
		/// present the user with workable information and this seems like it may be appropriate...
		/// </summary>
		/// <returns>BarrierTokenObject/returns>
		public async Task Login(string userName, string password)
		{
			if(string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
			{
				throw new Exception("User name and password must to be entered to log in.");
			}
			using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://www.xertonline.com/oauth/token"))
			{
				var base64authorization = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes("xert_public:xert_public"));
				request.Headers.TryAddWithoutValidation("Authorization", $"Basic {base64authorization}");

				var contentList = new List<string>();
				contentList.Add("grant_type=password");
				contentList.Add("username=" + userName);
				contentList.Add("password=" + password);
				request.Content = new StringContent(string.Join("&", contentList));
				request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded");
				HttpResponseMessage response = await _Client.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					string respString = await response.Content.ReadAsStringAsync();
					try
					{
						_Token = JsonConvert.DeserializeObject<BarrierToken>(respString);
					}
					catch (Exception ex)
					{
						if (null == _Token)
						{
							_Token = new BarrierToken()
							{
								error = "?",
								error_description = ex.Message
							};
						}
					}
				}
				else
				{
					StringBuilder sBErr = new StringBuilder("Login exception. status code: ");
					sBErr.Append(response.StatusCode.ToString());
					sBErr.Append("ReasonPhrase: ");
					sBErr.Append(response.ReasonPhrase);
					sBErr.Append(" Content: ");
					sBErr.Append(response.Content);
					sBErr.Append(" RequestMessage: ");
					sBErr.Append(response.RequestMessage);
					throw new Exception(sBErr.ToString());
				}
			}
			if (null != _Token)
			{
				if (!string.IsNullOrEmpty(_Token.error) && string.IsNullOrEmpty(_Token.access_token)) //error not empty and access token is empty
				{
					throw new Exception("XertClient LogIn failed! Error: " + _Token.error + ". Error description: " + _Token.error_description);
				}
				else if (string.IsNullOrEmpty(_Token.access_token))//access token empty 
				{
					throw new Exception("XertClient LogIn failed! The access token is empty");
				}
			}
			else
			{
				throw new Exception("XertClient LogIn failed! The login token is null but the cause is unknown");
			}
		}
			

		/// <summary>
		/// Returns a list of workouts. This function requires login'
		/// is required to obtain a token. The curl call is:
		/// curl -X GET "https://www.xertonline.com/oauth/workouts" -H "Authorization: Bearer <token>"
		/// I am refactoring any exception of the deserialize call but it is worth reconsidering this.
		/// </summary>
		/// <param name="token"></param>
		/// <returns></returns>
		public async Task<List<IXertWorkout>> GetUsersWorkouts()
		{
			if (null == _Token)
			{
				throw new Exception("GetUsersWorkouts() Exception! You must Log In before calling this function!");
			}
			using (var request = new HttpRequestMessage(new HttpMethod("GET"), "https://www.xertonline.com/oauth/workouts"))
			{
				request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + _Token.access_token);
				HttpResponseMessage response = await _Client.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					string respString = await response.Content.ReadAsStringAsync();
					try
					{
						UserWorkouts userWOs = JsonConvert.DeserializeObject<UserWorkouts>(respString);
						if (userWOs.success)
						{
							return userWOs.workouts.ToList<IXertWorkout>();
						}
						else
						{
							Exception execp = new Exception("There was an unknown error in GetUsersWorkouts. There were " +
								Convert.ToString(userWOs.workouts.Count) + " workouts.");
							throw execp;
						}
					}
					catch (Exception ex)
					{
						throw new Exception("GetUsersWorkouts() exception. " + ex.Message, ex);
					}
				}
				else
				{
					StringBuilder sBErr = new StringBuilder("GetUsersWorkouts exception. status code: ");
					sBErr.Append(response.StatusCode.ToString());
					sBErr.Append("ReasonPhrase: ");
					sBErr.Append(response.ReasonPhrase);
					sBErr.Append(" Content: ");
					sBErr.Append(response.Content);
					sBErr.Append(" RequestMessage: ");
					sBErr.Append(response.RequestMessage);
					throw new Exception(sBErr.ToString());
				}
			}
		}

		internal class BarrierToken
		{
			public string access_token { get; set; }
			public int expires_in { get; set; }
			public string token_type { get; set; }
			public string scope { get; set; }
			public string refresh_token { get; set; }
			public string error { get; set; }
			public string error_description { get; set; }
		}

		internal class UserWorkouts
		{
			public UserWorkouts()
			{
				success = false;
				workouts = new List<XertWorkout>();
			}
			public List<XertWorkout> workouts { get; set; }
			public bool success { get; set; }

		};

	};


	public class ValuePairIntString
	{
		public string type { get; set; }
		public float value { get; set; }
	}
	public class ValuePairStringString
	{
		public string type { get; set; }
		public string value { get; set; }
	}



	public class Set
	{
		public string DT_RowId { get; set; }
		public string sequence { get; set; }
		public string name { get; set; }
		public ValuePairIntString power { get; set; }
		public ValuePairStringString duration { get; set; }
		public ValuePairIntString rib_power { get; set; }
		public ValuePairStringString rib_duration { get; set; }
		public string interval_count { get; set; }
	};

	public class XertWorkout : IXertWorkout
	{
		
		public string _id { get; set; }
		public string path { get; set; }
		public string name { get; set; }
		public string description { get; set; }
		public string workout { get; set; }
		public List<Set> sets { get; set; }
		public string coach { get; set; }
		public bool recommended { get; set; }
		public string owner { get; set; }
		public string focus { get; set; }
		public float xss { get; set; }
		public string duration { get; set; }
		public bool valid_thumbnail { get; set; }
		public string thumb { get; set; }
		public string url { get; set; }
		public float advisorScore { get; set; }
		public float difficulty { get; set; }
		public string rating { get; set; }
	};


}
