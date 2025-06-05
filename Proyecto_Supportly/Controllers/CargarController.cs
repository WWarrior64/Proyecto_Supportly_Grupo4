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
        private const string _dropboxApiToken = "sl.u.AFyBJqRsl2FWg21_rsej8D8bk3JA98KMmY0TXBUPJQaGTUw0DoA0vLWG75Mc7LOSe-qliPEtSt6Lw5B4p6elMZhrBHUaMm7WaIcz_mwu48kDv7hGOWyNrVE_6kHh53FTJWlgVlIXsq9p8RhXxpwtsufWnRFac59ckCVwRtltMQfDJepXRjZouPm_AO4i6PTM2W4PkPh1mtHjtTViqFHQnPbV4GQwcUUVXy78ceu78QrNnjTDmYiLFto2kEcbMorBJ25ZeTfsv50MRUN8rPQY5oEB24styuF16DSjS8ThXUUV4QumCWlCKhpILepdOLipifmx9VyOecJTdgWNtr1NILH6Mh-hptd6Pb4Af1_C8OC0iQ-wAAzCfVq0pMgAv7EA6Ae7fpPAlCHoRzoF5sgtU4LAlWHmP_oRbrjuHWfMhVA7vv4Fuq4SBh9cFiuJwK7r2hshV78l8wS-aPJ1by_6thaZrsGx_MXO4RulyPezmG12fQTJxnmBCeTU75OPG1fzjJQmMNaKv9YcWZEimch-fb_C-u1fZX7x38I0RDLLGR7QxCkMrV1V-EQqCInTb5clUyDpHjk_z4iC-WNA6c802smeP2pnViTlkLG4xgrp9plYwjq2XBH32WLqGX-Ny8ncEr7xuSZadmCYLdeOfzAvCc__uQ-ajZHXpJPa86dPVD1k5FRNMQ7pGSHM4XpDFJbiJOxcIk_3qh4ReNIURArSX3cHSLBKmvB29DB9EqFnMDaTxqXfqS4QoQFcDng0ISmlmhU5qWcSoGFzwAWt_wfPaMLdDcWwlCOFnXqKr8sTG9-L0pVbz0DXPJ7fQVzoSbYID1g7nE0gNR3JX4RMxWheugvmZxElATlfBT_5e7WMeJ5boKITWzN9bGeTzWvxj2Z9YPPSNVAjQ1pcxXWWMYK3Tsot3g6DHRDda24S6BDwS1DL61PREatMkQBetYKLIXzDXQx57nJquvag9fs_4HKmBSsUa-nxQzqRR6YFbLXyoJoL_gagflwFar3FCmZr2SV43eUz_fi4DETqSYQW6NfkZMkONxrrl_LKWQ74JXNB12wUVfuhPjyufBhxrAHD04XehXQeLBxQ0srHrxzzDD_AzZhhXxCQR22M0D2-rt2_W8GM1xa8_ZYNxXFCKNEvtPUiZ_snXqdLGuJRwBbkYfbSqbTwuliCvB7yVdfPxZV4gLATafOu4qKR4qldl5yArvXTzCcDY6Qkz0pFoXBmkTQzBuYheAs6tOr2iuAdeTozeKHDbhYzM7bhTLBi--vaLtDCiskY-qd-YEObud9dzAGzzG3bUm_gsgHClTY9bFCh1EStdeCw_NyIhB8uOS5_4UfsFwiqGw-tkAeiJqlUKRrCpZqhBgxUM_ARXddxLTLrYveS8VZppyTSlhCSWekxS8tNO-NWI3Wps5paIkc7VJ6kPck6e-h8FdKOgy-dialCMSstPAwiL9e_yUKJ-1_qrMppuWgZnLVRUxX_GYHy_CcFhrw6B9LkQVf_Ac9mbwK4V--H2A";

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
