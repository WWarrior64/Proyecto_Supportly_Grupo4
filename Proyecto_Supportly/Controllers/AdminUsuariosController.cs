using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto_Supportly.Models;
using Microsoft.Extensions.Primitives;

public class AdminUsuariosController : Controller
{
    private readonly SupportDBContext _context;

    public AdminUsuariosController(SupportDBContext context)
    {
        _context = context;
    }

    /// <summary>
    /// GET: /AdminUsuarios/Index
    /// Carga la lista de usuarios, el mapa de roles y el contador inicial de referencias.
    /// </summary>
    public async Task<IActionResult> Index()
    {
        // 1. Obtener todos los usuarios
        var usuarios = await _context.Usuarios
            .AsNoTracking()
            .OrderBy(u => u.Nombre)
            .ToListAsync();

        // 2. Construir el diccionario de roles: RolID -> Nombre
        var roles = await _context.Roles
            .AsNoTracking()
            .ToListAsync();
        var rolesMap = roles.ToDictionary(r => r.RolID, r => r.Nombre);

        // 4. Paso de datos a ViewBag para la vista
        ViewBag.Usuarios = usuarios;
        ViewBag.RolesMap = rolesMap;
        ViewBag.UserRefStart = 1; // contador de referencia inicia en 1

        return View();
    }

    /// <summary>
    /// GET: /AdminUsuarios/CrearUsuarioInterno
    /// Muestra el formulario para crear un usuario interno.
    /// </summary>
    public IActionResult CrearUsuarioInterno()
    {
        // Cargar todos los roles (la vista decidirá cuáles internos vs externos, si aplica)
        ViewBag.RolesLista = new SelectList(
            _context.Roles.ToList(),
            "RolID", "Nombre"
        );
        return View();
    }

    /// <summary>
    /// POST: /AdminUsuarios/CrearUsuarioInterno
    /// Recibe los datos del formulario y crea un usuario interno.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearUsuarioInterno(Usuarios nuevoUsuario, string ConfirmPassword)
    {
        // Validar confirmación de contraseña
        if (nuevoUsuario.Contraseña != ConfirmPassword)
        {
            ModelState.AddModelError("ConfirmPassword", "Las contraseñas no coinciden.");
        }

        ModelState.Remove("TipoUsuario");
        nuevoUsuario.TipoUsuario = "Interno";
        nuevoUsuario.FechaCreacion = DateTime.Now;

        // Validar campos requeridos
        if (!ModelState.IsValid)
        {
            ViewBag.RolesLista = new SelectList(
                _context.Roles.AsNoTracking().ToList(),
                "RolID", "Nombre"
            );
            return View(nuevoUsuario);
        }

        try
        {

            _context.Usuarios.Add(nuevoUsuario);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Usuario interno creado correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch
        {
            TempData["ErrorMessage"] = "Ocurrió un error al crear el usuario interno.";
            ViewBag.RolesLista = new SelectList(
                _context.Roles.AsNoTracking().ToList(),
                "RolID", "Nombre"
            );
            return View(nuevoUsuario);
        }
    }


    /// <summary>
    /// GET: /AdminUsuarios/CrearUsuarioExterno
    /// Muestra el formulario para crear un usuario externo.
    /// </summary>
    public IActionResult CrearUsuarioExterno()
    {
        // Cargar lista de roles
        ViewBag.RolesLista = new SelectList(
                _context.Roles.AsNoTracking().ToList(),
                "RolID", "Nombre"
            );
        return View();
    }


    /// <summary>
    /// POST: /AdminUsuarios/CrearUsuarioExterno
    /// Recibe los datos del formulario y crea un usuario externo.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearUsuarioExterno(Usuarios nuevoUsuario, string ConfirmPassword)
    {
        // 1. Validar que las contraseñas coincidan
        if (nuevoUsuario.Contraseña != ConfirmPassword)
        {
            ModelState.AddModelError("ConfirmPassword", "Las contraseñas no coinciden.");
        }

        ModelState.Remove("TipoUsuario");
        nuevoUsuario.TipoUsuario = "Externo";
        nuevoUsuario.FechaCreacion = DateTime.Now;

        // 2. Validar ModelState
        if (!ModelState.IsValid)
        {
            // Si falla, volvemos a poblar el DropDown de Roles para que la vista se vuelva a renderizar.
            ViewBag.RolesLista = new SelectList(
                _context.Roles.AsNoTracking().ToList(),
                "RolID", "Nombre"
            );
            return View(nuevoUsuario);
        }

        try
        {

            // 4. Insertar en BD
            _context.Usuarios.Add(nuevoUsuario);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Usuario externo creado correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch
        {
            TempData["ErrorMessage"] = "Ocurrió un error al crear el usuario externo.";

            // Si algo sale mal en la inserción, volvemos a poblar Roles y retornar vista
            ViewBag.RolesLista = new SelectList(
                _context.Roles.AsNoTracking().ToList(),
                "RolID", "Nombre"
            );
            return View(nuevoUsuario);
        }
    }



    /// <summary>
    /// GET: /AdminUsuarios/EdicionUsuarioInterno/5
    /// Muestra el formulario para editar un usuario interno.
    /// </summary>
    public async Task<IActionResult> EdicionUsuarioInterno(int id)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null)
            return NotFound();

        // 1) Cargar todos los roles en ViewBag.RolesLista para el DropDown
        ViewBag.RolesLista = new SelectList(
            _context.Roles.AsNoTracking().ToList(),
            "RolID", "Nombre",
            usuario.RolID    // valor seleccionado por defecto
        );

        return View(usuario);
    }


    /// <summary>
    /// POST: /AdminUsuarios/EdicionUsuarioInterno/5
    /// Recibe los datos modificados y actualiza el usuario interno.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EdicionUsuarioInterno(int id, Usuarios usuarioEditado, string ContraseñaNueva)
    {
        if (id != usuarioEditado.UsuarioID)
            return BadRequest();

        if (!string.IsNullOrEmpty(ContraseñaNueva))
        {
            // Si el usuario sí ingresó nueva contraseña, asignamos:
            usuarioEditado.Contraseña = ContraseñaNueva;
        }
        // Si ContraseñaNueva viene vacío, mantenemos la contraseña original que ya vino en usuarioEditado.Contraseña

        //remover ambas entradas del ModelState para no validarlas:
        ModelState.Remove(nameof(usuarioEditado.Contraseña));      // No validar la contraseña “vieja”
        ModelState.Remove(nameof(ContraseñaNueva));                // No validar la contraseña nueva como campo obligatorio


        if (!ModelState.IsValid)
        {
            ViewBag.RolesLista = new SelectList(
                _context.Roles.ToList(),
                "RolID", "Nombre",
                usuarioEditado.RolID
            );
            return View(usuarioEditado);
        }

        try
        {
            _context.Usuarios.Update(usuarioEditado);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Usuario interno actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] = "Ocurrió un error al actualizar el usuario interno.";
            ViewBag.RolesLista = new SelectList(
                _context.Roles.ToList(),
                "RolID", "Nombre",
                usuarioEditado.RolID
            );
            return View(usuarioEditado);
        }
    }

    /// <summary>
    /// GET: /AdminUsuarios/EdicionUsuarioExterno/5
    /// Muestra el formulario para editar un usuario externo.
    /// </summary>
    public async Task<IActionResult> EdicionUsuarioExterno(int id)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null)
            return NotFound();

        // Cargar lista de roles
        ViewBag.RolesLista = new SelectList(
            _context.Roles.ToList(),
            "RolID", "Nombre",
            usuario.RolID
        );

        return View(usuario);
    }

    /// <summary>
    /// POST: /AdminUsuarios/EdicionUsuarioExterno/5
    /// Recibe los datos modificados y actualiza el usuario externo.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EdicionUsuarioExterno(int id, Usuarios usuarioEditado, string ContraseñaNueva)
    {
        if (id != usuarioEditado.UsuarioID)
            return BadRequest();

        // Si se ingresó una nueva contraseña, asignarla; si no, conservar la anterior
        if (!string.IsNullOrWhiteSpace(ContraseñaNueva))
        {
            usuarioEditado.Contraseña = ContraseñaNueva;
        }

        //remover ambas entradas del ModelState para no validarlas:
        ModelState.Remove(nameof(usuarioEditado.Contraseña));      // No validar la contraseña “vieja”
        ModelState.Remove(nameof(ContraseñaNueva));                // No validar la contraseña nueva como campo obligatorio

        if (!ModelState.IsValid)
        {
            ViewBag.RolesLista = new SelectList(
                _context.Roles.ToList(),
                "RolID", "Nombre",
                usuarioEditado.RolID
            );
            return View(usuarioEditado);
        }

        try
        {
            _context.Usuarios.Update(usuarioEditado);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Usuario externo actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] = "Ocurrió un error al actualizar el usuario externo.";
            ViewBag.RolesLista = new SelectList(
                _context.Roles.ToList(),
                "RolID", "Nombre",
                usuarioEditado.RolID
            );
            return View(usuarioEditado);
        }
    }


    /// <summary>
    /// POST: /AdminUsuarios/Delete/5
    /// Elimina un usuario y redirige a la lista.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null)
        {
            TempData["ErrorMessage"] = "Usuario no encontrado.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Usuario \"{usuario.Nombre}\" eliminado correctamente.";
        }
        catch
        {
            TempData["ErrorMessage"] = "Ocurrió un error al eliminar el usuario.";
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// GET: /AdminUsuarios/AsignarPermisos/5
    /// Carga la vista con la lista de todos los permisos y los IDs de los permisos ya asignados al usuario.
    /// </summary>
    public async Task<IActionResult> AsignarPermisos(int id)
    {
        // 1. Verificar que el usuario existe
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null)
            return NotFound();

        // 2. Obtener todos los permisos (entidad Permisos)
        var allPermisos = await _context.Permisos
            .AsNoTracking()
            .OrderBy(p => p.Nombre)
            .ToListAsync();

        // 3. Obtener lista de PermisoID que ya están asignados a este usuario
        var assignedIds = await _context.UsuariosPermisos
            .Where(up => up.UsuarioID == id)
            .Select(up => up.PermisoID)
            .ToListAsync();

        // 4. Enviar a la vista vía ViewBag
        ViewBag.AllPermissions = allPermisos;                   // List<Permisos>
        ViewBag.AssignedPermissionIDs = assignedIds;            // List<int>
        ViewBag.UsuarioID = id;
        ViewBag.UsuarioNombre = usuario.Nombre;

        return View();
    }

    /// <summary>
    /// POST: /AdminUsuarios/AsignarPermisos/5
    /// Lee los checkboxes marcados y actualiza la tabla UsuariosPermisos.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AsignarPermisos(int id, IFormCollection form)
    {
        // 1. Verificar que el usuario existe
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null)
            return NotFound();

        // 2. Obtener todos los PermisoID
        var allPermissionIds = await _context.Permisos
            .AsNoTracking()
            .Select(p => p.PermisoID)
            .ToListAsync();

        // 3. Construir lista de IDs de permisos marcados como “Permitido”
        var nuevosIdsPermitidos = new List<int>();
        foreach (var permisoID in allPermissionIds)
        {
            var checkboxName = $"Permitido_{permisoID}";
            // form[checkboxName] es StringValues; puede contener {"false","true"} o solo {"true"}.
            if (form.TryGetValue(checkboxName, out StringValues vals))
            {
                // Si entre los valores viene “true”, entendemos que el checkbox estaba marcado
                if (vals.Any(v => v == "true" || v == "on"))
                {
                    nuevosIdsPermitidos.Add(permisoID);
                }
            }
        }

        // 4. Eliminar todas las asignaciones previas para este usuario
        var existentes = _context.UsuariosPermisos
            .Where(up => up.UsuarioID == id);
        _context.UsuariosPermisos.RemoveRange(existentes);

        // 5. Insertar los nuevos
        foreach (var pid in nuevosIdsPermitidos)
        {
            _context.UsuariosPermisos.Add(new UsuariosPermisos
            {
                UsuarioID = id,
                PermisoID = pid
            });
        }

        // 6. Guardar cambios
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Permisos actualizados correctamente.";

        return RedirectToAction(nameof(Index));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _context.Dispose();
        base.Dispose(disposing);
    }
}