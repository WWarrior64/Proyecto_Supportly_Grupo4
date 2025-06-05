// CargarController.cs
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using Dropbox.Api;
using Dropbox.Api.Files;
using System.Threading.Tasks;

namespace Proyecto_Supportly.Controllers
{
    public class CargarController : Controller
    {
        // <-- Reemplaza aquí con tu token de Dropbox (mejor guardarlo en appsettings.json o variable de entorno)
        private const string _dropboxApiToken = "sl.u.AFx_RRPPJovRU_3X7H419OowD3m0aItYwMC_cebBUnyf8Gg2TdGJ2VnXhzkO3rZc_u6kwX8dHYOVefimY6As1yrOLXqr2pSCE8G4r4krhOx9u5z70RQMwwR7t5hpV1ioAI1d5IQrQh9IgJZrNgaDtVUHxX2aSmOjefM1cdpAB2FxDSZTWX1AKLaneST8OM2SDtDx_q2cE4kRx2jcpg9BBLzQw7cxQnF1lFJQm0sWuHXQuEwVvDUonbei81PKn9jHA02o347vM-PApLd-KW32c-Zeq5rtMDYC0xsYHJAhEhO-sH4D_ui4nO3i_ZqszjgOVh-qZIC6FQGe6gsngHpKow-x8JXwC5MK2t24Q0JSpBalE3vdK_2FZXdFJGgaEZ6R4_ejkpxosfNnuBH6rO7kvSh0cuWgNp1W7glLfGgu0JDi3xxrlZ6sHGEPwMBdEFb0p2Dd2TrkIGBHu2pSkAphCNjcG3XkcUTPLuvrs5GOtVJ7U9wU0iPC72j70brbvWfzMriLaQxGFJVOvrioGUWUUINaqwb8LoJgLSx93k3Zr-c3aBYpyjn614hgS3s-lGYCnahlrTLoDYy7XeBD4dlyu0W8h8Bx23rLhhW8FX9l7HEX_LHB3Zs2--UDr23-KbrEWb-oJfPKJHGz_FuCwJvz67VbElDmIl0dR5px_u6haHx2vSPQ5YfoHgWMVh6KgE1fhQTTcyxyqwaDJr53P1iC5G2nCrLh0Bg5t85LQFvuRxX8aTsWaC3KUTbTYKM2YGdsoWc0SpkH94QG-CfzWFORWMUbTci86ZJQEoqr59AXXFYSTy_GFhrWTncyS9TebV78jMnjE5EoTDZd9gsjLLmum6koVYU7_0WCo237jxDzXEUtQKL1nAMsVwGriOOwVid-0xKXV7YnMd43e1L27pxQOMyK1W4mFKoIV_s6ORXgVuVbp3OifaGGV8Sqhcgr3rg6HGct-tN53Hnouvk73rqufuUhWNxQOzO7eXznbVCioKKXFkbpTpopPgQZXs05JHoRFV8n0Yhaw8Pzyb1suDmR6isOmWto3xm628H03ms8THL4x45OhKQFleWnVE0CG_A_c_S6KxAXI5B8fa4Jd1LWfceHOKuzWRJyOqu5gr32aqYDtBvyXHT6DIK69H3JubFS650AQ6ZdXYwGN3Q_5DEB1QPmCmsm7R5mbR5jfRFuEg9HhiYEeGc_uSwiwi7sBjU8N_gT6oGAbQ1o-YLhLrEkviEqgzhOFfaznHla-bp_BnxnUMjJls-VH8Kxzml5dwYcvuIZ7PRC7QlQCTvWnV8JMnJ3bWyQyRDsJLaE48nD46vCOjU6XuqQKIayrYkslKS1qJKLqJK9YMzUotbOUCaDJSD-w8GLnTELMMhRZCUXLHid2np4Z8PWXCawtH1-yQVl0RKLGk6-qQvTshf9JO5kcFHmo87GNY8y32dpbdBNotKIF2k5L9Z9iEYfUQwYl6qLePTssaN547y8D6g4eaddlih8ZYJ_1CPgGnVi8A2Wu3DJgg";

        /// <summary>
        /// GET: /Cargar/UploadFile
        /// Muestra simplemente la vista de subida (que luego embebemos en CrearTicket.cshtml).
        /// </summary>
        public ActionResult UploadFile(
                int ticketId,
                int? userId,
                string returnController,
                string returnAction)
        {
            // Para que el POST tenga toda la info:
            ViewBag.TicketId = ticketId;
            ViewBag.UserId = userId;
            ViewBag.ReturnController = returnController;
            ViewBag.ReturnAction = returnAction;
            return View();
        }

        /// <summary>
        /// POST: /Cargar/UploadFile
        /// Recibe un archivo, lo sube a Dropbox y genera un enlace compartido.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadFile(IFormFile file, int ticketId,
            int? userId,
            string returnController,
            string returnAction)
        {
            if (file == null || file.Length == 0)
            {
                TempData["UploadMessage"] = "No se seleccionó ningún archivo.";
                TempData["MessageType"] = "error";
                return RedirectToAction("CrearTicket", "CrearTicket");
            }

            try
            {
                using (var dbx = new DropboxClient(_dropboxApiToken))
                {
                    // Generamos la ruta dentro de Dropbox
                    var dropboxPath = $"/SupportlyTickets/{Path.GetFileName(file.FileName)}";

                    // Subimos el archivo
                    using (var stream = file.OpenReadStream())
                    {
                        var uploadResult = await dbx.Files.UploadAsync(
                            path: dropboxPath,
                            mode: WriteMode.Overwrite.Instance,
                            body: stream
                        );
                    }

                    // Creamos un enlace compartido (para vista directa)
                    var sharedLinkMetadata = await dbx.Sharing.CreateSharedLinkWithSettingsAsync(dropboxPath);

                    var dropboxLink = sharedLinkMetadata.Url.Replace("?dl=0", "?raw=1");

                    // Guardamos en TempData para que la vista CrearTicket los reciba
                    TempData["UploadMessage"] = "Archivo subido correctamente a Dropbox.";
                    TempData["MessageType"] = "success";
                    TempData["DropboxLink"] = dropboxLink;
                    TempData["UploadedFileName"] = Path.GetFileName(file.FileName);
                }
            }
            catch (Exception ex)
            {
                TempData["UploadMessage"] = $"Error al subir el archivo: {ex.Message}";
                TempData["MessageType"] = "error";
            }

            // Redirigimos a la acción GET CrearTicket para que la vista recargue y muestre el enlace
            return RedirectToAction(returnAction, returnController, new { id = ticketId, userId = userId });
        }
    }
}
