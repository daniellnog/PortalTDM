using LaefazWeb.Models;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using TDMWeb.Models.VOs;

namespace WebServices.Controllers
{
    [Authorize]
    public class StatusController : ApiController
    {
        private DbEntities db = new DbEntities();

        //private void SetPrincipal(IPrincipal principal)
        //{
        //    Thread.CurrentPrincipal = principal;
        //    if (HttpContext.Current != null)
        //    {
        //        HttpContext.Current.User = principal;
        //    }
        //}

        //public static void Register(HttpConfiguration config)
        //{
        //    config.Filters.Add(new AuthorizeAttribute());
        //}

        [HttpGet]
        public List<StatusVO> getStatus ()
        {
            List<Status> listaStatus = (from myRow in db.Status
                                          select myRow).ToList();
            List<StatusVO> listaUsuariosVO = new List<StatusVO>();

            foreach (Status s in listaStatus)
            {
                listaUsuariosVO.Add(new StatusVO { Id = s.Id, Descricao = s.Descricao });
            }
            return listaUsuariosVO;
        }
    }
}
