namespace Mango.Web.Service.IService
{
    public interface ITokenProvider
    {
        public string GetToken();
        public void ClearToken();
        public void SetToken(string token);
    }
}
