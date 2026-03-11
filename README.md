# ByteChat

**ByteChat** es una aplicación de chat desarrollada en **Unity** que implementa comunicación en red utilizando los protocolos **TCP y UDP**.  
El sistema permite enviar **mensajes de texto, imágenes y archivos PDF**, demostrando las diferencias entre protocolos orientados a conexión (TCP) y no orientados a conexión (UDP).

El proyecto fue desarrollado con fines educativos para comprender el funcionamiento de **sockets, transmisión de datos y arquitectura cliente-servidor** dentro de un entorno interactivo.

---

# Características

- Comunicación **Cliente / Servidor**
- Selección de protocolo **TCP o UDP**
- Envío de **mensajes de texto**
- Envío de **imágenes**
- Envío de **archivos PDF**
- Interfaz gráfica en **Unity UI**
- Manejo de **eventos asincrónicos**
- Sistema de **encabezados de paquetes (packet header)** para identificar tipos de datos

---

# Arquitectura del Proyecto

El sistema sigue una arquitectura **cliente-servidor basada en sockets**.

```
Cliente
   │
   │  (TCP / UDP)
   │
Servidor
```

### Componentes principales

| Componente | Descripción |
|------------|-------------|
| TCPServer | Maneja conexiones TCP entrantes y la recepción de datos |
| TCPClient | Conecta al servidor y envía mensajes o archivos |
| UDPServer | Recibe datagramas UDP y gestiona mensajes |
| UDPClient | Envía mensajes UDP al servidor |
| UIManager | Controla la interfaz gráfica del chat |
| ProtocolSelector | Permite elegir el protocolo de comunicación |
| ConnectionManager | Determina si la instancia actúa como servidor o cliente |

---

# Explicación de TCP y UDP

## TCP (Transmission Control Protocol)
En este proyecto TCP permite enviar:

- Mensajes
- Imágenes
- PDFs

---

## UDP (User Datagram Protocol)
En este proyecto UDP se utiliza principalmente para **mensajes de texto**, ya que los archivos grandes requieren fragmentación de paquetes.

---

# Protocolo de Mensajes

Para poder enviar distintos tipos de datos (texto, imágenes, PDF), se implementó un **encabezado de paquete personalizado**.

Cada paquete tiene la siguiente estructura:

```
[TYPE][SIZE][DATA]
```

| Campo | Tamaño | Descripción |
|------|------|-------------|
| TYPE | 1 byte | Tipo de mensaje |
| SIZE | 4 bytes | Tamaño de los datos |
| DATA | variable | Contenido del mensaje |

### Tipos de mensaje

| Tipo | Valor |
|-----|-----|
| Texto | 0 |
| Imagen | 1 |
| PDF | 2 |


# Envío de Imágenes

Las imágenes se convierten a **PNG o JPG** antes de enviarse.

```csharp
byte[] imageBytes = image.EncodeToPNG();
```

El receptor reconstruye la imagen usando:

```csharp
Texture2D texture = new Texture2D(2,2);
texture.LoadImage(data);
```

---

# Envío de Archivos PDF

Los archivos PDF se envían como **arreglos de bytes**.

```csharp
byte[] pdfBytes = File.ReadAllBytes(path);
```

Al recibirlos se guardan localmente:

```csharp
File.WriteAllBytes(path, pdfBytes);
```

El usuario puede abrirlos mediante un botón en la interfaz.

---

# Interfaz de Usuario

La interfaz del chat fue implementada con **Unity UI**.

Incluye:

- Selección de protocolo
- Campo de entrada de mensajes
- Área de chat con scroll
- Botones para imágenes y PDFs

---

# Flujo de Conexión

1. El usuario selecciona el protocolo.
3. Se establece la conexión.
4. El sistema determina si actúa como **cliente o servidor**.

En el caso de **UDP**, se realiza un pequeño handshake:

```
Client → CONNECT
Server → CONNECTED
```

---

# Proceso de Investigación

Durante el desarrollo del proyecto se investigaron los siguientes aspectos:

### Manejo de sockets en Unity

Unity no incluye un sistema de networking TCP/UDP directo para aplicaciones personalizadas, por lo que se utilizaron las librerías:

```
System.Net
System.Net.Sockets
```

### Comunicación asíncrona

Para evitar bloquear el hilo principal de Unity se utilizaron:

```
async / await
```

y loops de recepción como:

```csharp
_ = ReceiveLoop();
```

Esto permite escuchar mensajes en segundo plano.

### Envío de diferentes tipos de archivos

Se investigaron técnicas para enviar diferentes formatos de datos:

| Tipo | Método |
|-----|------|
| Texto | UTF8 Encoding |
| Imagen | EncodeToPNG / EncodeToJPG |
| PDF | File.ReadAllBytes |

Todos los datos se envían como **arreglos de bytes** dentro del protocolo definido.

---

# Estructura del Proyecto

```
Assets
│
├── Chat_TCP_UDP
│   ├── Scenes
│   │   └── 0 (Proyecto)
│   │
│   ├── Scripts
│   │   ├── Interface
│   │   │   ├── ProtocolSelector.cs
│   │   │   ├── IClient.cs
│   │   │   ├── IServer.cs
│   │   │   ├── IChatConnection.cs
│   │   │   ├── UIManager.cs
│   │   │   └── ChatManager.cs
│   │   │
│   │   ├── TCP
│   │   │   ├── TCPClient.cs
│   │   │   └── TCPServer.cs
│   │   │
│   │   └── UDP
│   │       ├── UDPClient.cs
│   │       └── UDPServer.cs
```

---

# Instrucciones de Ejecución

## Requisitos

- Unity 6+
- .NET compatible con Unity
- Dos instancias del proyecto o un Unity editor y una instancia a la vez

---

## Pasos

1. Abrir el proyecto en Unity
2. Ejecutar la escena principal
3. Seleccionar protocolo (TCP o UDP)
4. Introducir IP y puerto
5. Presionar **Connect**

Una instancia actuará como **servidor** y la otra como **cliente** dependiendo de quien entra primero a la app.

---

# Tecnologías Utilizadas

- Unity Engine
- C#
- Sockets TCP / UDP
- Async / Await
- Unity UI
- TextMeshPro

---

# Video de Demostración


```
https://youtu.be/5-HWgX3C4Yg

```

---

# Autor

Proyecto desarrollado por:

**Natalia Martin**
---
**Alejandra Acevedo**
