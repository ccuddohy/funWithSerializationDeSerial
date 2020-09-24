using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using XertClient;
using System.IO;
//using Newtonsoft.Json;
//using System.Text.Json;
using System.Linq;
using System.Net.Http.Headers;

namespace ConsoleApp3core
{
	class Program
	{
		static void Main(string[] args)
		{
			try
			{
				IXertClient _client = new Client();
				Console.WriteLine("One Moment");
				var Tone = Task.Run(async () => await _client.Login("chris", "xxx"));
				Tone.Wait();
				Console.WriteLine("working");
				var WOs = Task.Run(async () => await _client.GetUsersWorkouts());
				Console.WriteLine("Originally there were {0} workouts returned", WOs.Result.Count);

				SerializeWorkoutsToFile(WOs.Result, "workouts.json");
				List<IXertWorkout> iback = DeserializeWorkoutsFromFile("workouts.json");
				Console.WriteLine("Using Newtonsoft with conversion there were {0} workouts returned", iback.Count);

				SerializeWorkoutsToFile_BasicNS(WOs.Result, "workouts_BasicNS.json");
				List<IXertWorkout> iback_NS = DeserializeWorkoutsFromFile_BasicNS("workouts_BasicNS.json");
				Console.WriteLine("Using Newtonsoft with casting there were {0} workouts returned", iback_NS.Count);

				SerializeWorkoutsToFile_sysCast(WOs.Result, "workouts_SysCast.json");
				List<IXertWorkout> iback_SysCast = DeserializeWorkoutsFromFile_sysCast("workouts_SysCast.json");
				Console.WriteLine("Using system.json with casting there were {0} workouts returned", iback_NS.Count);


			}
			catch (Exception except)
			{
				Console.WriteLine("\n" + except.Message);
			}
			Console.WriteLine("\nany key to close");
			Console.ReadKey();
		}


		// vvvvvv  Newtonsoft using conversion   vvvvv
		static void SerializeWorkoutsToFile(List<IXertWorkout> workouts, string fileName)
		{
			Newtonsoft.Json.JsonSerializer serializer = new Newtonsoft.Json.JsonSerializer();
			serializer.Converters.Add(new Newtonsoft.Json.Converters.JavaScriptDateTimeConverter());
			serializer.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
			serializer.TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto;
			serializer.Formatting = Newtonsoft.Json.Formatting.Indented;

			using (StreamWriter sw = new StreamWriter(fileName))
			using (Newtonsoft.Json.JsonWriter writer = new Newtonsoft.Json.JsonTextWriter(sw))
			{
				serializer.Serialize(writer, workouts, typeof(List<IXertWorkout>));
			}
		}
		static List<IXertWorkout> DeserializeWorkoutsFromFile(string fileName)
		{
			List<IXertWorkout> workouts = Newtonsoft.Json.JsonConvert.DeserializeObject<List<IXertWorkout>>(File.ReadAllText(fileName), new Newtonsoft.Json.JsonSerializerSettings
			{
				TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto,
				NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
			});
			return workouts;
		}
		// ^^^^^^^^  Newtonsoft using conversion  ^^^^^^^^

		// vvvvvvvvvv   System.Text.Json using a converter vvvvvvvvvvvvvvvv
		static void SerializeWorkoutsToFile_sysConv(List<IXertWorkout> workouts, string fileName)
		{

		}
		// ^^^^^^^^   System.Text.Json using a converter  ^^^^^^^^

		//  vvvvvv   Newtonesoft cheating by creating a concrete object first  vvvvvv 
		static void SerializeWorkoutsToFile_BasicNS(List<IXertWorkout> workouts, string fileName)
		{
			File.WriteAllText(fileName, Newtonsoft.Json.JsonConvert.SerializeObject(workouts));
		}
		static List<IXertWorkout> DeserializeWorkoutsFromFile_BasicNS(string path)
		{
			string jsonstr = File.ReadAllText(path);
			List<XertWorkout> wos = Newtonsoft.Json.JsonConvert.DeserializeObject<List<XertWorkout>>(jsonstr);
			return wos.ToList<IXertWorkout>(); ;
		}
		// ^^^^^^^^   Newtonesoft cheating by creating a concrete object first  ^^^^^^^^




		// vvvvvvvvvv   System.Text.Json cheating by creating a concrete object first vvvvvvvvvvvvvvvv
 		static void SerializeWorkoutsToFile_sysCast(List<IXertWorkout> workouts, string fileName)
		{
			string jsonString = System.Text.Json.JsonSerializer.Serialize(workouts);
			File.WriteAllText(fileName, jsonString);
		}
		static List<IXertWorkout> DeserializeWorkoutsFromFile_sysCast(string fileName)
		{
			string jsonString = File.ReadAllText(fileName);
			List<XertWorkout> lstWorkouts = System.Text.Json.JsonSerializer.Deserialize<List<XertWorkout>>(jsonString);
			return lstWorkouts.ToList<IXertWorkout>();
		}
		// ^^^^^^^^ System.Text.Json cheating by creating a concrete object first ^^^^^^^^
	}
}
