using System.Threading.Tasks;

namespace BlackWatch.Daemon.Features.Messari
{
    public interface IMessariApiClient
    {
        Task<AssetListResponse> GetAssetsAsync(int page = 1);
    }
}
