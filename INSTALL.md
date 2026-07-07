# Instalar Glide en otra máquina

## Opción 1: Solo usar el ejecutable (usuario final)

La forma más fácil: no necesitas nada especial.

1. Descarga el `.exe` desde las **[Releases](https://github.com/uSoloMid/Glide/releases)** (una vez que se publique v0.1).
2. Haz doble-clic en `Glide.exe`.
3. Se abre la ventana de ajustes en el primer arranque; luego aparece el icono en la bandeja.

**Requisitos:**
- Windows 10 build 2004+ (mayo 2020) o Windows 11
- .NET 10 Runtime (viene preinstalado en Win11; en Win10 se descarga automáticamente si no lo tienes)

Para autoarranque silencioso en el tray cada vez que inicies sesión: dentro de Glide, activa **"Start with Windows"** en la sección **General**.

## Opción 2: Compilar desde el código

Si quieres cambiar la configuración, añadir funcionalidad o construir tu propia versión.

**Requisitos:**
- Windows 10 2004+ o Windows 11
- [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download) (IDE opcional: Visual Studio 2022 Community)
- Git

**Pasos:**

```bash
git clone https://github.com/uSoloMid/Glide.git
cd Glide
dotnet build Glide.slnx -c Release
dotnet test  Glide.slnx -c Release   # Verifica los 46 tests
```

El ejecutable compilado estará en:
```
src/Glide.UI/bin/Release/net10.0-windows/Glide.exe
```

## Qué se necesita en cada máquina

| Escenario | .NET Runtime | .NET SDK | Git |
|---|---|---|---|
| Solo ejecutable (usuario) | ✓ (se descarga si falta) | ✗ | ✗ |
| Compilar desde source | ✓ | ✓ | ✓ |

## Instalación portátil

Puedes mover el `.exe` compilado a cualquier carpeta, pendrive o carpeta de sincronización en la nube. Glide guarda sus ajustes en:
```
%APPDATA%\Glide\settings.json
```

Si quieres compartir configuración entre máquinas, sincroniza esa carpeta.

## Solución de problemas

**La app no inicia:**
- Asegúrate de que tienes Windows 10 build 2004+ o superior.
- En Win10, instala [.NET 10 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) manualmente.
- Busca `glide.log` en `%LOCALAPPDATA%\Glide` para ver el error exacto.

**El zoom no funciona:**
- Verifica que **"Enable Glide"** esté activado en la sección **General** de los ajustes.
- Algunos juegos en fullscreen exclusivo necesitan ser añadidos a la lista de exclusiones (sección **Applications**).

**Necesito más ayuda:**
- Abre un [issue](https://github.com/uSoloMid/Glide/issues) en GitHub.
