using Microsoft.EntityFrameworkCore;

namespace Proyecto_Supportly.Models
{
    public class SupportDBContext: DbContext
	{
		public SupportDBContext(DbContextOptions options) : base(options)
        {

        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UsuariosPermisos>()
                .HasKey(up => new { up.UsuarioID, up.PermisoID });

            base.OnModelCreating(modelBuilder);
        }

        public DbSet<Tickets> Tickets { get; set; }
        public DbSet<Usuarios> Usuarios { get; set; }
        public DbSet<Asignaciones> Asignaciones { get; set; }
        public DbSet<Categorias> Categorias { get; set; }
        public DbSet<Estados> Estados { get; set; }
        public DbSet<Adjuntos> Adjuntos { get; set; }
        public DbSet<Comentarios> Comentarios { get; set; }
        public DbSet<Notificaciones> Notificaciones { get; set; }
        public DbSet<Roles> Roles { get; set; }
        public DbSet<Permisos> Permisos { get; set; }
        public DbSet<UsuariosPermisos> UsuariosPermisos { get; set; }
    }
}
