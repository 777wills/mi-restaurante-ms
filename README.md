# MiRestaurante API

API REST de un restaurante desarrollada con C#, ASP.NET Core y .NET 10.
Permite consultar, crear, actualizar y retirar platos del menú. Usa Entity Framework
Core y PostgreSQL 18 para persistir los datos.

Esta guía explica cómo preparar un equipo y ejecutar el proyecto en desarrollo.
**No necesitas instalar PostgreSQL directamente en tu equipo ni crear sus tablas a
mano**: Docker ejecuta PostgreSQL y la API aplica las migraciones.

> La API no incluye una aplicación web para clientes del restaurante. Sí incluye
> **Scalar**, una interfaz similar a Swagger UI para consultar la documentación y
> ejecutar peticiones desde el navegador. Actualmente no implementa autenticación;
> no expongas este entorno de desarrollo a Internet.

## Contenido

1. [Qué se ejecuta y qué necesitas](#1-qué-se-ejecuta-y-qué-necesitas)
2. [Instalar las herramientas](#2-instalar-las-herramientas)
3. [Clonar el repositorio](#3-clonar-el-repositorio)
4. [Restaurar y compilar](#4-restaurar-y-compilar)
5. [Iniciar PostgreSQL](#5-iniciar-postgresql)
6. [Ejecutar la API](#6-ejecutar-la-api)
7. [Probar los endpoints con Scalar](#7-probar-los-endpoints-con-scalar)
8. [Conectarse a la base de datos](#8-conectarse-a-la-base-de-datos)
9. [Datos semilla y migraciones](#9-datos-semilla-y-migraciones)
10. [Trabajo diario y persistencia](#10-trabajo-diario-y-persistencia)
11. [Ejecutar pruebas y controles de calidad](#11-ejecutar-pruebas-y-controles-de-calidad)
12. [Solución de problemas](#12-solución-de-problemas)
13. [Estructura y documentación del proyecto](#13-estructura-y-documentación-del-proyecto)
14. [Resumen de arranque](#14-resumen-de-arranque)

## 1. Qué se ejecuta y qué necesitas

```text
Navegador / Scalar / cliente HTTP
              |
              v
API .NET ejecutada en tu equipo
http://localhost:5110
              |
              v
PostgreSQL 18 ejecutado en Docker
localhost:5432
              |
              v
Volumen persistente de Docker: postgres-data
```

Docker Compose levanta **solo PostgreSQL**. La API se ejecuta con `dotnet run`
o desde el IDE, no dentro de un contenedor.

| Herramienta | Para qué sirve | Obligatoria |
|---|---|---|
| Git | Descargar el código y trabajar con versiones | Sí |
| SDK de .NET 10 | Restaurar dependencias, compilar y ejecutar la API | Sí |
| Docker Engine o Docker Desktop | Ejecutar PostgreSQL y los contenedores de pruebas | Sí |
| Docker Compose, comando `docker compose` | Administrar el PostgreSQL de desarrollo | Sí |
| Navegador | Usar Scalar | Recomendado |
| Visual Studio Code y C# Dev Kit, o un IDE compatible con .NET 10 | Editar y depurar | Opcional |
| Extensión PostgreSQL de VS Code, DBeaver o pgAdmin | Explorar tablas y datos | Opcional |

Necesitas acceso a Internet para clonar, descargar paquetes NuGet y obtener la
imagen de PostgreSQL. Los puertos `5432` y `5110` deben estar disponibles;
`7087` también si eliges el perfil HTTPS.

## 2. Instalar las herramientas

Si ya tienes alguna herramienta instalada, comprueba su versión antes de instalarla
otra vez. Los comandos de instalación de esta sección son para tu equipo; no forman
parte del arranque habitual del proyecto.

### 2.1. Git

**Windows**

1. Descarga [Git para Windows](https://git-scm.com/downloads/win).
2. Ejecuta el instalador y habilita su uso desde la terminal.
3. Cierra y vuelve a abrir PowerShell o VS Code para actualizar el `PATH`.

**macOS**

Ejecuta:

```bash
xcode-select --install
```

Acepta la instalación de las herramientas de línea de comandos. Si ya están
instaladas, no necesitas repetirla. También puedes seguir la
[guía de Git para macOS](https://git-scm.com/downloads/mac).

**Ubuntu**

```bash
sudo apt update
sudo apt install git curl ca-certificates
```

**Verificación en cualquier sistema**

```bash
git --version
```

Debe mostrar una versión de Git, no un error de comando inexistente.

### 2.2. SDK de .NET 10

La versión requerida se declara en [global.json](./global.json):

- SDK solicitado: `10.0.112`.
- Política `rollForward`: `latestFeature`, que permite versiones compatibles
  posteriores del SDK dentro de `10.0`.
- Un SDK de .NET 8 o 9 no sustituye al SDK de .NET 10.
- Instala el **SDK**, no solamente el runtime de .NET o ASP.NET Core.

**Windows y macOS**

1. Abre [Descargas de .NET 10](https://dotnet.microsoft.com/download/dotnet/10.0).
2. Selecciona un SDK compatible con la versión del repositorio.
3. Elige el instalador de tu sistema y arquitectura: por ejemplo, Windows x64,
   macOS Arm64 para Apple Silicon o macOS x64 para Intel.
4. Ejecuta el instalador y vuelve a abrir la terminal.

**Ubuntu y otras distribuciones Linux**

Sigue la [guía oficial de instalación de .NET en Linux](https://learn.microsoft.com/dotnet/core/install/linux)
para los paquetes y requisitos de tu distribución.

Como alternativa para desarrollo, puedes instalar el SDK solicitado en tu usuario,
sin instalarlo globalmente, mediante el
[script oficial de instalación](https://learn.microsoft.com/dotnet/core/tools/dotnet-install-script):

```bash
curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/mirestaurante-dotnet-install.sh
bash /tmp/mirestaurante-dotnet-install.sh --version 10.0.112 --install-dir "$HOME/.dotnet"
rm /tmp/mirestaurante-dotnet-install.sh
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$DOTNET_ROOT:$PATH"
```

El script no instala las dependencias nativas del sistema: revisa la guía de tu
distribución si informa de bibliotecas faltantes. Los dos `export` afectan a la
terminal actual; para conservarlos, añádelos a la configuración de tu shell
(`~/.bashrc` si usas Bash o `~/.zshrc` si usas Zsh) y abre una terminal nueva.

**Verificación**

```bash
dotnet --list-sdks
dotnet --info
```

Después de clonar, ejecuta también `dotnet --version` desde la raíz del repositorio:
así compruebas la selección del SDK según [global.json](./global.json).

### 2.3. Docker y Docker Compose

El comando utilizado en esta guía es **`docker compose`**, con espacio. No necesitas
instalar por separado PostgreSQL ni el comando antiguo `docker-compose`.

#### Windows: Docker Desktop

1. Consulta los requisitos e instala
   [Docker Desktop para Windows](https://docs.docker.com/desktop/setup/install/windows-install/).
2. Habilita la virtualización del equipo si está desactivada.
3. Si no tienes WSL 2, abre PowerShell como administrador y ejecuta:

   ```powershell
   wsl --install
   ```

4. Reinicia cuando Windows lo solicite, completa la configuración de WSL y ejecuta
   `wsl --update` si Docker lo requiere.
5. Instala y abre Docker Desktop. Usa el motor WSL 2 y contenedores **Linux**,
   necesarios para la imagen `postgres:18`.
6. Espera hasta que Docker Desktop indique que el motor está iniciado.

Puedes trabajar desde PowerShell o desde WSL. Si eliges WSL, habilita la integración
de tu distribución en Docker Desktop e instala Git y el SDK de .NET dentro de esa
distribución. No mezcles instrucciones de Bash con PowerShell.

#### macOS: Docker Desktop

1. Descarga [Docker Desktop para Mac](https://docs.docker.com/desktop/setup/install/mac-install/)
   según tu procesador: Apple Silicon o Intel.
2. Abre el archivo `.dmg` y arrastra Docker a Aplicaciones.
3. Abre Docker, completa la configuración inicial y espera a que inicie el motor.

Docker Desktop incluye Docker Compose. Revisa sus condiciones de licencia si vas
a utilizarlo en una organización.

#### Ubuntu: Docker Engine y el complemento Compose

Estos pasos son para Ubuntu compatible con la
[guía oficial de Docker Engine](https://docs.docker.com/engine/install/ubuntu/).
Si ya tienes otra instalación de Docker, revisa primero la sección de conflictos
de esa guía; no desinstales ni borres datos de una instalación existente sin revisarla.
En Debian, Fedora u otro sistema utiliza las instrucciones oficiales de su distribución.

**1. Añade la clave y el repositorio oficial:**

```bash
sudo apt update
sudo apt install ca-certificates curl
sudo install -m 0755 -d /etc/apt/keyrings
sudo curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
sudo chmod a+r /etc/apt/keyrings/docker.asc

sudo tee /etc/apt/sources.list.d/docker.sources <<EOF
Types: deb
URIs: https://download.docker.com/linux/ubuntu
Suites: $(. /etc/os-release && echo "${UBUNTU_CODENAME:-$VERSION_CODENAME}")
Components: stable
Architectures: $(dpkg --print-architecture)
Signed-By: /etc/apt/keyrings/docker.asc
EOF
```

**2. Instala e inicia Docker:**

```bash
sudo apt update
sudo apt install docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
sudo systemctl enable --now docker
sudo docker run --rm hello-world
```

**3. Configura acceso sin `sudo`, si corresponde a tu equipo:**

```bash
sudo usermod -aG docker "$USER"
```

Cierra completamente tu sesión de usuario e inicia sesión otra vez. Después abre
una terminal nueva. Esto también permite que las pruebas con Testcontainers
accedan a Docker desde tu usuario.

> El grupo `docker` concede privilegios equivalentes a administrador sobre el equipo.
> En entornos compartidos, consulta al administrador o evalúa el
> [modo rootless](https://docs.docker.com/engine/security/rootless/).
> No soluciones permisos aplicando `chmod 666` al socket de Docker.

#### Verificar Docker en cualquier sistema

Con Docker Desktop abierto o Docker Engine iniciado:

```bash
docker --version
docker compose version
docker info
docker run --rm hello-world
```

`docker info` debe mostrar información del servidor sin errores de conexión o
permisos. `hello-world` comprueba que puedes descargar y ejecutar un contenedor.

### 2.4. Editor y cliente de base de datos opcionales

Puedes instalar [Visual Studio Code](https://code.visualstudio.com/) con las
extensiones **C# Dev Kit** y **PostgreSQL** de Microsoft.
También puedes usar [DBeaver Community](https://dbeaver.io/download/) o
[pgAdmin](https://www.pgadmin.org/download/) para explorar PostgreSQL.

Estas herramientas no sustituyen al servidor PostgreSQL: el servidor lo iniciará
Docker en el paso 5.

## 3. Clonar el repositorio

Abre una terminal en la carpeta donde guardas tus proyectos:

```bash
git clone https://github.com/777wills/mi-restaurante-ms.git
cd mi-restaurante-ms
```

Si el repositorio es privado, necesitas acceso y autenticarte con GitHub. No
escribas tokens ni contraseñas en la URL de clonación.

Todos los comandos siguientes, salvo que se indique lo contrario, se ejecutan
desde esta carpeta, donde están [MiRestaurante.slnx](./MiRestaurante.slnx) y
[compose.yaml](./compose.yaml).

Comprueba el estado y la versión de .NET:

```bash
git status
dotnet --version
```

Opcionalmente, abre el proyecto en VS Code:

```bash
code .
```

Si `code` no está disponible, utiliza **Archivo → Abrir carpeta** en VS Code.

> Una clonación descarga únicamente cambios confirmados y publicados en GitHub,
> no archivos sin confirmar de otro equipo. Si el equipo desarrolla una función
> en otra rama, consulta `git branch -r` y cambia a la rama indicada antes de
> continuar. Si no aparecen el CRUD o las semillas descritas aquí, comprueba que
> tu rama contiene esos cambios; reinstalar Docker no los incorpora.

## 4. Restaurar y compilar

```bash
dotnet restore MiRestaurante.slnx
dotnet tool restore
dotnet build MiRestaurante.slnx
```

- `restore` descarga los paquetes NuGet declarados por los proyectos.
- `tool restore` instala las herramientas **locales** declaradas en
  [.config/dotnet-tools.json](./.config/dotnet-tools.json): `dotnet-ef` y
  ReportGenerator. No instales versiones globales para este proyecto.
- `build` compila la solución. Debe terminar sin errores ni advertencias.

No necesitas Node.js, npm ni un archivo `.env` para el arranque documentado.
La configuración local ya está en
[appsettings.Development.json](./src/MiRestaurante.Api/appsettings.Development.json).
ASP.NET Core no carga automáticamente un archivo `.env` en este proyecto.

## 5. Iniciar PostgreSQL

### 5.1. Crear e iniciar el contenedor

```bash
docker compose up -d postgres
docker compose ps
```

La primera ejecución descarga `postgres:18` y puede tardar varios minutos.
`-d` deja el contenedor ejecutándose en segundo plano.

[compose.yaml](./compose.yaml) configura:

| Parámetro | Valor local |
|---|---|
| Imagen | `postgres:18` |
| Servicio de Compose | `postgres` |
| Puerto del equipo | `5432` |
| Puerto del contenedor | `5432` |
| Base de datos | `mirestaurante` |
| Usuario | `mirestaurante` |
| Contraseña exclusivamente de desarrollo | `mirestaurante_dev` |
| Volumen de datos | `postgres-data` |

El nombre real del volumen puede incluir un prefijo del proyecto Compose.
No necesitas conocer el nombre del contenedor para usar estos comandos.

### 5.2. Esperar a que PostgreSQL esté listo

Que el contenedor figure como iniciado no significa que la base ya acepte conexiones.
Compruébalo:

```bash
docker compose exec postgres pg_isready -U mirestaurante -d mirestaurante
```

Espera una respuesta similar a `accepting connections`. Si aún está iniciando,
espera unos segundos y repite. Para consultar el arranque:

```bash
docker compose logs --tail 100 postgres
```

**No ejecutes `CREATE DATABASE` ni crees tablas manualmente.** La imagen crea
`mirestaurante` al inicializar un volumen vacío; la API crea su esquema mediante
migraciones en el siguiente paso.

> Las variables `POSTGRES_DB`, `POSTGRES_USER` y `POSTGRES_PASSWORD` inicializan
> un volumen nuevo. Cambiarlas en Compose no modifica automáticamente una base
> ya existente. Los datos no están dentro del repositorio ni se descargan con Git.

## 6. Ejecutar la API

### 6.1. Perfil HTTP recomendado para empezar

En una terminal desde la raíz:

```bash
dotnet run --project src/MiRestaurante.Api --launch-profile http
```

Este comando:

1. Compila si es necesario.
2. Activa el entorno **Development** mediante
   [launchSettings.json](./src/MiRestaurante.Api/Properties/launchSettings.json).
3. Conecta con PostgreSQL usando la configuración de desarrollo.
4. Aplica las migraciones pendientes y carga las semillas incluidas en ellas.
5. Escucha en `http://localhost:5110`.

Mantén esta terminal abierta. Para detener la API pulsa `Ctrl+C`.
El navegador no se abre automáticamente.

### 6.2. Verificar el funcionamiento

Abre estas direcciones:

| Recurso | URL | Resultado esperado |
|---|---|---|
| Salud y conexión a BD | <http://localhost:5110/health> | `Healthy`, HTTP 200 |
| Interfaz Scalar | <http://localhost:5110/scalar> | Documentación interactiva |
| OpenAPI JSON | <http://localhost:5110/openapi/v1.json> | Documento de la API |
| Menú | <http://localhost:5110/api/v1/dishes> | Lista JSON de platos |

Desde otra terminal, en Bash:

```bash
curl -i http://localhost:5110/health
curl -i http://localhost:5110/api/v1/dishes
```

En Windows PowerShell utiliza `curl.exe` para evitar el alias de versiones antiguas:

```powershell
curl.exe -i http://localhost:5110/health
curl.exe -i http://localhost:5110/api/v1/dishes
```

La raíz `http://localhost:5110/` no tiene página de inicio; un 404 allí no implica
que la API esté averiada. Usa `/scalar` o `/health`.

### 6.3. HTTPS opcional

Si necesitas HTTPS, detén la instancia HTTP y prepara el certificado local:

```bash
dotnet dev-certs https --trust
dotnet run --project src/MiRestaurante.Api --launch-profile https
```

Acepta la solicitud de confianza si el sistema la muestra. En Linux puede requerir
configuración adicional según el navegador y la distribución; consulta la
[documentación de certificados de desarrollo](https://learn.microsoft.com/aspnet/core/security/enforcing-ssl).
Puedes seguir usando el perfil HTTP si solo necesitas desarrollo local.

Con el perfil HTTPS, abre <https://localhost:7087/scalar> y
<https://localhost:7087/health>. Este perfil también declara el puerto HTTP `5110`,
que puede redirigir las peticiones a HTTPS.

## 7. Probar los endpoints con Scalar

Scalar cumple el papel de una interfaz como Swagger UI, pero este proyecto **no usa
Swagger UI ni Swashbuckle**. Scalar y OpenAPI están disponibles solo en Development.

1. Abre <http://localhost:5110/scalar>.
2. Selecciona una operación del menú.
3. Abre su panel para probar la petición.
4. Introduce los parámetros o el cuerpo JSON.
5. Envía la petición y revisa el código HTTP y la respuesta.

### Operaciones actuales

| Método | Ruta | Acción |
|---|---|---|
| GET | `/api/v1/dishes` | Listar platos no eliminados |
| GET | `/api/v1/dishes?available=true` | Listar solo los disponibles |
| GET | `/api/v1/dishes?available=false` | Listar solo los no disponibles |
| GET | `/api/v1/dishes/{id}` | Consultar un plato |
| POST | `/api/v1/dishes` | Crear un plato |
| PUT | `/api/v1/dishes/{id}` | Reemplazar los datos de un plato |
| DELETE | `/api/v1/dishes/{id}` | Retirar un plato mediante borrado lógico |

Para crear un plato con `POST`, utiliza este cuerpo en Scalar:

```json
{
  "name": "Changua",
  "description": "Caldo de leche con huevo, cebolla larga y cilantro.",
  "price": 12000,
  "imageUrl": null,
  "isAvailable": true
}
```

La respuesta correcta es `201 Created`. Copia el `id` devuelto para consultar,
actualizar o eliminar ese plato; no asumas que será siempre `6`.
Para `PUT` envía el cuerpo completo con los campos requeridos.

- `name`, `price` e `isAvailable` son obligatorios.
- `name` admite hasta 100 caracteres y no puede quedar vacío.
- `description` es opcional, con un máximo de 500 caracteres.
- El precio está en **pesos colombianos (COP)**, debe ser mayor que cero, como máximo
  `99999999.99` y tener hasta dos decimales. En JSON usa punto decimal y no incluyas
  símbolos de moneda ni separadores de miles.
- `imageUrl` es opcional; si se proporciona, debe ser una URL absoluta HTTP o HTTPS,
  de hasta 2048 caracteres.
- El nombre no puede repetirse entre platos no eliminados, sin distinguir
  mayúsculas de minúsculas.
- `DELETE` responde `204 No Content` y conserva la fila con `DeletedAt`.
  El plato deja de aparecer en las consultas de la API.

Los errores de entrada responden `400`; un plato inexistente, `404`; un nombre
duplicado, `422`. Los errores HTTP se representan con ProblemDetails.

También tienes peticiones de ejemplo en
[MiRestaurante.Api.http](./src/MiRestaurante.Api/MiRestaurante.Api.http).
Puedes usarlas con un editor o extensión que ejecute archivos `.http`.

## 8. Conectarse a la base de datos

La API debe haberse iniciado al menos una vez para que existan las tablas.
Usa una conexión **PostgreSQL**, no SQL Server.

### 8.1. Extensión PostgreSQL de VS Code

Abre la vista PostgreSQL y selecciona **Add Connection**. En la pestaña
**Parameters**, completa:

| Campo de la pantalla | Valor |
|---|---|
| Server Name | `localhost` |
| Authentication Type | `Password` |
| User Name | `mirestaurante` |
| Password | `mirestaurante_dev` |
| Save Password | Opcional, solo si confías en tu equipo |
| Database Name | `mirestaurante` |
| Connection Name | `MiRestaurante local`, o un nombre descriptivo |
| Server Group | Puedes conservar `Servers` |

En **Advanced**, usa puerto `5432` y SSL Mode `Disable` para este entorno local.
Pulsa **Test Connection** y después **Save & Connect**.

El nombre descriptivo va en **Connection Name**. **Server Name debe ser
`localhost`**, no el nombre del proyecto o de la universidad.

Expande la conexión hasta **mirestaurante → Schemas → public → Tables**
(los nombres pueden variar según el cliente). Actualiza el explorador si ya
estaba abierto antes de ejecutar las migraciones.

### 8.2. DBeaver o pgAdmin

Crea una conexión de tipo PostgreSQL con los mismos valores:

- Host o dirección: `localhost`.
- Puerto: `5432`.
- Base de datos: `mirestaurante`.
- Usuario: `mirestaurante`.
- Contraseña local: `mirestaurante_dev`.
- SSL: desactivado para este entorno.

En pgAdmin, el nombre de registro se escribe en la pestaña **General** y los
datos del servidor en **Connection**. Usa `mirestaurante` como base de
mantenimiento si ese es el campo disponible.

### 8.3. Consultas para comprobar los datos

En el editor SQL de tu cliente, conectado a `mirestaurante`, ejecuta:

```sql
SELECT "Id", "Name", "Price", "IsAvailable", "DeletedAt"
FROM public."Dishes"
ORDER BY "Id";
```

Las comillas dobles son necesarias porque las tablas y columnas conservan
mayúsculas. Para consultar solo los platos no eliminados:

```sql
SELECT "Id", "Name", "Price", "IsAvailable"
FROM public."Dishes"
WHERE "DeletedAt" IS NULL
ORDER BY "Name";
```

Para revisar las migraciones aplicadas:

```sql
SELECT "MigrationId"
FROM public."__EFMigrationsHistory"
ORDER BY "MigrationId";
```

### 8.4. Acceder sin instalar un cliente gráfico

El contenedor ya incluye `psql`:

```bash
docker compose exec postgres psql -U mirestaurante -d mirestaurante
```

En la consola de `psql`:

```text
\dt
SELECT "Id", "Name", "Price", "IsAvailable" FROM "Dishes" ORDER BY "Id";
\q
```

`\dt` lista las tablas y `\q` sale de la consola.

> `localhost` es correcto cuando el cliente se ejecuta en tu equipo, como en
> estas instrucciones. Si ejecutas pgAdmin u otro cliente en un contenedor,
> `localhost` apunta a ese contenedor, no a PostgreSQL. La configuración de un
> cliente contenerizado requiere una red compartida y queda fuera de este arranque.

## 9. Datos semilla y migraciones

### 9.1. Menú inicial

La migración `AddDishes` incluye estos datos:

| Id | Plato | Precio COP | Disponible |
|---|---|---:|---|
| 1 | Bandeja paisa | 38.000 | Sí |
| 2 | Ajiaco santafereño | 32.000 | Sí |
| 3 | Sancocho de hueso | 28.000 | Sí |
| 4 | Lechona | 30.000 | No |
| 5 | Arepa de huevo | 8.000 | Sí |

Se definen con `HasData` en
[DishConfiguration.cs](./src/MiRestaurante.Infrastructure/Configurations/DishConfiguration.cs)
y se insertan al aplicar su migración, automáticamente al arrancar en Development.
No necesitas ejecutar un script de semillas adicional.

En una base recién migrada, `/api/v1/dishes` devuelve cinco platos y
`/api/v1/dishes?available=true` devuelve cuatro.

**Reiniciar la API no duplica las semillas ni restaura las que hayas modificado
o eliminado.** Las migraciones ya aplicadas quedan registradas.
Para agregar platos de trabajo utiliza `POST` desde Scalar.

### 9.2. Cambios futuros del esquema o las semillas

Solo si estás desarrollando un cambio que requiere una migración:

1. Modifica el modelo o su configuración en la capa correspondiente.
2. Genera una nueva migración; no edites una migración ya compartida para
   intentar que se vuelva a ejecutar.
3. Revisa el código generado y confirma la migración junto con el cambio.

Ejemplo de generación, reemplazando `DescribeYourChange` por un nombre en inglés
que describa el cambio real:

```bash
dotnet ef migrations add DescribeYourChange --project src/MiRestaurante.Infrastructure --startup-project src/MiRestaurante.Api
```

Reinicia la API en Development para aplicar las migraciones pendientes.
No crees ni modifiques el esquema con DDL manual.

### 9.3. Entornos distintos de Development

Fuera de Development, la API **no aplica migraciones automáticamente** y no expone
Scalar, OpenAPI ni CORS abierto.

Para un despliegue, genera un script idempotente y aplícalo mediante el
procedimiento autorizado del entorno:

```bash
dotnet ef migrations script --idempotent --project src/MiRestaurante.Infrastructure --startup-project src/MiRestaurante.Api
```

La cadena de conexión se configura con la variable de entorno
`ConnectionStrings__DefaultConnection`. Las credenciales de otros entornos no
deben versionarse. Este README no es una guía de despliegue seguro en producción.

## 10. Trabajo diario y persistencia

### Iniciar otro día

1. Abre Docker Desktop o asegúrate de que Docker Engine esté iniciado.
2. Abre una terminal en el repositorio.
3. Ejecuta:

   ```bash
   docker compose up -d postgres
   docker compose exec postgres pg_isready -U mirestaurante -d mirestaurante
   dotnet run --project src/MiRestaurante.Api --launch-profile http
   ```

Espera a que `pg_isready` acepte conexiones antes de arrancar la API.
No tienes que clonar ni reinstalar herramientas cada vez.
Si descargaste cambios de dependencias, repite `dotnet restore` y
`dotnet tool restore` según corresponda.

### Detener sin perder los datos

Detén la API con `Ctrl+C` y luego:

```bash
docker compose stop postgres
```

También puedes retirar el contenedor y la red de Compose conservando el volumen:

```bash
docker compose down
```

Ambas opciones conservan los datos. Después utiliza `docker compose up -d postgres`
para volver a iniciar.

### Reiniciar PostgreSQL y consultar registros

```bash
docker compose restart postgres
docker compose logs --tail 100 postgres
```

La salida de la API aparece en la terminal de `dotnet run`, no en los registros
de Docker.

### Recrear la base desde cero: operación destructiva opcional

> **Advertencia:** el siguiente comando elimina el volumen PostgreSQL de este
> proyecto Compose y todos sus datos. No es un paso del arranque normal.
> Haz una copia de seguridad si necesitas conservarlos y comprueba que estás
> en la carpeta y el proyecto correctos.

Detén primero la API. Solo si deseas borrar toda la base local:

```bash
docker compose down --volumes
docker compose up -d postgres
docker compose exec postgres pg_isready -U mirestaurante -d mirestaurante
```

Cuando PostgreSQL esté listo, vuelve a ejecutar:

```bash
dotnet run --project src/MiRestaurante.Api --launch-profile http
```

Esto aplica las migraciones en una base nueva y recupera las semillas originales.
No uses esta operación para solucionar una contraseña incorrecta sin antes
revisar la configuración y valorar los datos que perderías.

## 11. Ejecutar pruebas y controles de calidad

Las pruebas usan xUnit v3 sobre **Microsoft.Testing.Platform**, no VSTest.
Ejecuta los comandos desde la raíz y con Docker accesible para tu usuario.

### Compilación y formato

```bash
dotnet build MiRestaurante.slnx
dotnet format --verify-no-changes MiRestaurante.slnx
```

### Todas las pruebas

```bash
dotnet test --solution MiRestaurante.slnx
```

Las pruebas de integración y funcionales levantan sus propios contenedores
PostgreSQL 18 con Testcontainers. No requieren el servicio de Compose ni utilizan
la base `mirestaurante` de desarrollo. Sí necesitan el motor Docker activo.
La primera ejecución puede tardar más por la descarga de la imagen.

Para ejecutar un proyecto o una clase:

```bash
dotnet test --project tests/MiRestaurante.Architecture.Tests
dotnet test --project tests/MiRestaurante.Architecture.Tests --filter-class "*ControllerDataAccessTests"
```

El segundo comando ejecuta solo la clase indicada. Puedes sustituir su patrón
para seleccionar otras clases existentes. No uses `--filter` de VSTest.

### Cobertura de líneas: mínimo del 80 %

Los informes se guardan en la carpeta ignorada `TestResults`.
Antes de una nueva medición elimina **solo esa carpeta de resultados** desde el
explorador, para no mezclar informes antiguos con los nuevos. No borres el
repositorio ni otras carpetas.

```bash
dotnet test --solution MiRestaurante.slnx --results-directory TestResults --coverlet --coverlet-output-format cobertura
dotnet reportgenerator -reports:"TestResults/coverage.cobertura.*.xml" -targetdir:TestResults/report -reporttypes:"TextSummary;Html" "-assemblyfilters:+MiRestaurante.Domain;+MiRestaurante.Application;+MiRestaurante.Infrastructure;+MiRestaurante.Api" minimumCoverageThresholds:lineCoverage=80
```

El parámetro `minimumCoverageThresholds:lineCoverage=80` se escribe **sin guion
inicial**. ReportGenerator fusiona los informes y termina con código distinto de
cero si no se alcanza el umbral.
Consulta el resumen en `TestResults/report/Summary.txt` o abre
`TestResults/report/index.html` para el informe HTML.

Antes de integrar cambios deben pasar compilación, formato, pruebas y cobertura.
Las reglas completas están en la
[constitución del proyecto](./.specify/memory/constitution.md).

## 12. Solución de problemas

| Problema | Qué revisar |
|---|---|
| `git`, `dotnet` o `docker` no se reconoce | Instala la herramienta y abre una terminal nueva para actualizar el `PATH`. |
| No se encuentra un SDK compatible o aparece `NETSDK1045` | Ejecuta `dotnet --list-sdks`; instala un SDK .NET 10 compatible con `global.json`, no solo el runtime. |
| Falla la restauración de NuGet o herramientas | Revisa acceso a Internet, proxy y el error concreto de NuGet. No cambies versiones al azar ni instales herramientas globales para ocultar el error. |
| `Cannot connect to the Docker daemon` | Abre Docker Desktop o inicia Docker Engine; comprueba `docker info`. |
| `permission denied` al acceder a Docker en Linux | Revisa pertenencia al grupo `docker` y vuelve a iniciar sesión. No uses permisos abiertos sobre el socket. |
| `docker compose` no existe | Instala el complemento Compose o actualiza Docker Desktop. |
| El puerto `5432` ya está ocupado | Puede existir otro PostgreSQL o contenedor. Identifícalo y decide si debes detenerlo o cambiar el puerto local como se explica abajo. |
| La API falla conectando a PostgreSQL al arrancar | Ejecuta `docker compose ps`, revisa sus registros y espera a `pg_isready`. En Development, las migraciones requieren BD disponible antes del arranque. |
| Error de autenticación de PostgreSQL | Comprueba usuario, contraseña, puerto y volumen existente; cambiar las variables de Compose no cambia las credenciales de un volumen inicializado. |
| VS Code muestra `Couldn't reach 'FET'` u otro nombre descriptivo | En **Server Name** usa `localhost`; deja el nombre descriptivo para **Connection Name**. |
| No aparecen tablas | Arranca la API con `--launch-profile http`, comprueba que no falle la migración y actualiza la conexión a `mirestaurante`, esquema `public`. |
| `relation "dishes" does not exist` en una consulta | Usa `"Dishes"` con comillas y mayúscula; comprueba que las migraciones estén aplicadas en la base correcta. |
| No aparecen cinco semillas | Comprueba la rama, la migración `AddDishes` y si los registros ya fueron modificados o eliminados. Reiniciar no repite migraciones aplicadas. |
| `/health` responde `503 Unhealthy` | PostgreSQL no está accesible desde la API; revisa servicio y cadena de conexión. El endpoint comprueba conexión, no verifica que estén todas las tablas. |
| `/scalar` u `/openapi/v1.json` responde 404 | Confirma el entorno Development usando el perfil `http` o `https`; esas rutas no se exponen en Production. |
| `/` responde 404 | Es normal: no hay página de inicio. Abre `/scalar` o `/health`. |
| El puerto `5110` o `7087` está ocupado | Comprueba si ya ejecutas la API en otra terminal o en el depurador. Detén esa instancia o ajusta la URL de tu perfil local. |
| El navegador advierte del certificado HTTPS | Configura la confianza del certificado de desarrollo o usa el perfil HTTP. |
| Aparece `Failed to determine the https port for redirect` con el perfil HTTP | No hay un puerto HTTPS definido en ese perfil. Puedes usar HTTP local o arrancar el perfil HTTPS con su certificado configurado. |
| Las pruebas de integración no pueden iniciar contenedores | Comprueba `docker info` desde el mismo usuario y terminal que ejecuta los tests, y acceso a la imagen `postgres:18`. |
| Scalar no termina de cargar aunque OpenAPI responde | Revisa la consola y red del navegador; comprueba si el proxy o la red bloquean la descarga de recursos externos de la interfaz. |

### Cambiar el puerto PostgreSQL si `5432` está ocupado

Solo si necesitas mantener otro servidor en `5432`:

1. Cambia en [compose.yaml](./compose.yaml) el mapeo a `"5433:5432"`.
   Cambia únicamente el puerto del equipo; el del contenedor sigue siendo `5432`.
2. Cambia `Port=5432` a `Port=5433` en
   [appsettings.Development.json](./src/MiRestaurante.Api/appsettings.Development.json).
3. Ejecuta `docker compose up -d postgres` para recrear el contenedor con el mapeo
   actualizado, sin eliminar su volumen.
4. Usa `5433` también en el cliente gráfico.
5. Espera a que PostgreSQL esté listo y reinicia la API.

No incluyas cambios locales accidentales de puertos o credenciales en un commit.
Para configuraciones particulares también puedes sobrescribir la cadena mediante
`ConnectionStrings__DefaultConnection`; una variable existente tiene prioridad
sobre la configuración JSON. Si el archivo parece correcto pero la conexión no,
comprueba si tu terminal o IDE define esa variable.

### Información para reportar un problema

Antes de pedir ayuda, recopila el comando que falló, su error, tu sistema operativo
y la salida relevante de:

```bash
dotnet --info
docker compose version
docker compose ps
docker compose logs --tail 100 postgres
```

Revisa y oculta contraseñas, tokens, cadenas de conexión y cualquier dato sensible
antes de compartir registros.

## 13. Estructura y documentación del proyecto

La solución tiene cuatro proyectos de producción:

| Carpeta | Responsabilidad |
|---|---|
| [src/MiRestaurante.Domain](./src/MiRestaurante.Domain/) | Entidades del dominio |
| [src/MiRestaurante.Application](./src/MiRestaurante.Application/) | Servicios, DTOs, validaciones e interfaces |
| [src/MiRestaurante.Infrastructure](./src/MiRestaurante.Infrastructure/) | EF Core, PostgreSQL, configuración y migraciones |
| [src/MiRestaurante.Api](./src/MiRestaurante.Api/) | Controladores HTTP, configuración y arranque |
| [tests](./tests/) | Pruebas unitarias, de integración, funcionales y de arquitectura |
| [specs](./specs/) | Especificaciones y guías de validación por funcionalidad |

Referencias importantes:

- [Constitución: fuente única de verdad](./.specify/memory/constitution.md).
- [Configuración del SDK y del runner de pruebas](./global.json).
- [Herramientas locales](./.config/dotnet-tools.json).
- [Configuración de PostgreSQL en Docker](./compose.yaml).
- [Configuración de la API en Development](./src/MiRestaurante.Api/appsettings.Development.json).
- [Perfiles y puertos de ejecución](./src/MiRestaurante.Api/Properties/launchSettings.json).
- [Guía de validación de persistencia](./specs/003-persistence-local-env/quickstart.md).
- [Guía de validación del CRUD de platos](./specs/004-dishes-crud/quickstart.md).

El código y los identificadores se escriben en inglés; los mensajes al usuario y
la documentación, en español. Antes de contribuir, consulta la constitución:
incluye arquitectura, TDD, convenciones y puertas de calidad.

## 14. Resumen de arranque

Después de instalar Git, el SDK .NET 10 y Docker con Compose, y con el motor
Docker en ejecución:

```bash
git clone https://github.com/777wills/mi-restaurante-ms.git
cd mi-restaurante-ms
dotnet restore MiRestaurante.slnx
dotnet tool restore
dotnet build MiRestaurante.slnx
docker compose up -d postgres
docker compose exec postgres pg_isready -U mirestaurante -d mirestaurante
```

Cuando `pg_isready` indique `accepting connections`:

```bash
dotnet run --project src/MiRestaurante.Api --launch-profile http
```

Abre **<http://localhost:5110/scalar>** para probar la API y
**<http://localhost:5110/health>** para verificar la conexión a PostgreSQL.
