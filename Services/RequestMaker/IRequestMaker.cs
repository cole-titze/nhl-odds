using Entities.ServiceModels;

namespace Services.RequestMaker
{
	public interface IRequestMaker
	{
        public Task<ServiceResponse> MakeRequest(string url, string query);
    }
}

