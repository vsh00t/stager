# Custom Stager

## Descripción General
Este proyecto implementa un stager personalizado en C# que funciona como un cargador de segunda etapa para ejecutar cargas útiles remotas. El stager está diseñado para descargar, desencriptar, descomprimir y ejecutar código arbitrario en memoria sin escribir en el disco.

## Funcionalidades Principales

### 1. Descarga Sigilosa
- Intenta descargar una carga útil desde una URL base predefinida probando múltiples extensiones de archivo web
- Utiliza validación de certificados SSL personalizada para permitir conexiones seguras
- Implementa un mecanismo de recuperación para manejar fallos en las descargas

### 2. Procesamiento de Carga Útil
- **Desencriptación**: Utiliza el algoritmo AES para desencriptar la carga útil con una clave y vector de inicialización predefinidos
- **Descompresión**: Descomprime los datos mediante GZip para optimizar la transmisión
- **Ejecución en Memoria**: Ejecuta el código directamente en memoria utilizando técnicas de asignación de memoria dinámica sin escribir en disco

### 3. Evasión de Detección
- Implementa extensiones web comunes para confundir sistemas de protección
- Elimina mensajes de salida para evitar registros
- Utiliza técnicas de inyección en memoria para evitar detección basada en disco

## Arquitectura Técnica
- Aprovecha PInvoke para llamar a funciones nativas del sistema:
  - `VirtualAlloc`: Asigna memoria ejecutable
  - `CreateThread`: Inicia un nuevo hilo para la ejecución
  - `WaitForSingleObject`: Sincroniza la ejecución de hilos

## Requisitos del Sistema
- Sistema operativo Windows
- .NET Framework 
- Acceso a Internet para la descarga de la carga útil

## Nota de Seguridad
Este código se proporciona únicamente con fines educativos y de investigación en seguridad informática. Su uso debe limitarse a entornos controlados y autorizados. El mal uso de esta herramienta podría violar leyes locales e internacionales.

## Autor
- vsh00t