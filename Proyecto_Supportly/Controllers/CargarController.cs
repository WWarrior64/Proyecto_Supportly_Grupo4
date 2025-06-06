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
        private const string _dropboxApiToken = "sl.u.AFzrJO8_BHjy05WKg7_9PoDkHokEjeiCYAzsD7dfM0eAUpd5HofweC5JF3ZvFKgdhnhkVJdL8Sc03ndOEOYLpAS69J5AbEPILT8zUxyqwH54N7noMRxh8KOV49nqjlfhd1zXu0d1IC-XhOfnRB2EfqLY4d53xSB_OTC_TXhx0N0WiFb1xuh2BUGgQHklXfqTc_0bR5lT25vnPDzXEWrLd1PcoKiFP9cPtWuqhywCMaSM7mJAi16fY3HwKmSRcG0Ur5PRnEds0jG6o9RF_vQnQIIhZn6AGHaG6f-gchs6DCNJnWI7RyNsp_fWhX9aqJO9bnHTyLZT02X5jcQJJqS0VYre8VrNwPdibZmUoPxXmxfTrObqwq_2iYOXG7DE2fYiwZXjzucyTe8qv1yQwbuYh-_ROkgUMfWrhPlJcUL879HrcY1jruVnwGQBjlVINVs5v-hVxm38S1KP0UIKLt6q9Hv-mfzZyk1ecL_xF0hI3IFiGGb0oR56KIMI6qla3nAKaI_SHmKhoxNLkHzBx51glCjqu-_kY8qEjxu1zzV-NfNoAmJyn1cOBqxxD5_WLvOXu6soMMtQT6hohRFduq6xhWDM_fbF3NVRkhWvKvHc2lIwSyc0dDuqXQ4N2pKg0CvpgrE5y-y0XXdizU-IA_pPqT8zBuLCFxmaQWKRb810juLYZBimt_dy3btfiGz9a0ibleyf-uoRf1GqwenhkJeZVSkhMjxD1wYAjRpPCARQK3PwyQ8KtDBNHusQTbLBCpoSmDpqsohAwTqnz4tyuCEoV3bwEyYTgLmJcg6KPIllUfze4VH58aoJWicICZLp8314Ukz8HfS3Hg3mwf3DQxTDWP4NFTzbZxKnK2kl4VsskNV9vQa5twaE0auvk5cWh76QPhTwZK3OXs_bJpvpJ8JvITL3EwWTUWp3_OQoxkgJyOnWtonhvktJB8x2vgGPapVHZxTUoiiEKawFPTpzbPka4gTw7ShE-g_y_SN9GL-WuXSH4qwhuZVYtnpntAyzkN7qcK_WLy2aPXpnNURR1mL97g35_jen4XVVmiaRi9FUcSTwUielYeDfeF2R8bifz085JOpQbqumXz3P-C_hJGI0-WdZ4dAvKlvE54PZsFC-aCKppztMGKPWqW784YvRhafyx71DI1xOtrckYdz3W4X5rQHWcxJrRbZ5mDYiEoTKPldBh2p9tT8juNFDo6lDDKYWzCHcCri99aTD7K-W5C4_4WdLEZX1iGrd8gyqEAZEUpFg49B6htQ7FC0N22tfwbYA0SPYEM1AmEyv3Pb34CH0Zk9uwsdN80M3-MRzexdbu_vMFRO53WZjaEt4cF_8ybD1goYf-xxT-7ZEbST9IM9-BWlZ9q480usSCsKlwsC9wfe7W1HsYCKUdDrc44G7oIwPwip1dNTBaNgA61_GxgL9-WWFkr9eGLWX-Co5IBn-Ho__jfqYuaVzNinIroAzpQAJPsVqnhrzc3GDXYjSpLX9_kc2lgP5zBJQyrGiARCRg7c5Hg";

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
