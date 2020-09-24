using System.Collections.Generic;
using System.Threading.Tasks;

namespace XertClient
{
	public interface IXertClient
	{
		Task<List<IXertWorkout>> GetUsersWorkouts();
		Task Login(string userName, string password);
	}
}