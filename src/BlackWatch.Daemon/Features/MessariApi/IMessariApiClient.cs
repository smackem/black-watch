using System.Threading.Tasks;

namespace BlackWatch.Daemon.Features.MessariApi
{
    public interface IMessariApiClient
    {
        Task<AssetListResponse> GetAssetsAsync(int page = 1);
    }
}
