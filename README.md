# OperacionTools 📦🚀
> Suite de herramientas digitales optimizadas para la automatización de procesos, auditoría física de mercancías y validación de guías en tiempo real para operaciones logísticas.

[![C#](https://img.shields.io/badge/Language-C%23%2010.0-blue.svg)](https://learn.microsoft.com/es-es/dotnet/csharp/)
[![.NET Core](https://img.shields.io/badge/Framework-.NET%206.0%20%2F%207.0%20%2F%208.0-purple.svg)](https://dotnet.microsoft.com/download)
[![WPF](https://img.shields.io/badge/UI-WPF%20%20Desktop-orange.svg)](https://learn.microsoft.com/es-es/dotnet/desktop/wpf/)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](https://opensource.org/licenses/MIT)

---

## 📋 Descripción General
**OperacionTools** es una aplicación de escritorio nativa para Windows diseñada específicamente para resolver cuellos de botella en el proceso de inventarios y auditorías de bodega. 

Su principal fortaleza es el **Módulo de Inventario Malla**, el cual permite a los operarios realizar un cruce instantáneo de información (*Conciliación*) entre las guías físicas escaneadas mediante pistolas de código de barras y el inventario teórico del sistema importado desde archivos maestros de Excel. El sistema detecta automáticamente faltantes de unidades, excesos en mercancías multizona y sobrantes físicos no reportados en el sistema antes del despacho a las demas areas.

---

## ✨ Características Principales
* **Cruce Automático de Inventarios (Malla General):** Algoritmo optimizado capaz de procesar miles de registros en microsegundos mitigando la complejidad computacional mediante indexación avanzada (`HashSet`).
* **Resiliencia Operativa (Backup Automático Preventivo):** Guarda de forma automática el progreso de las lecturas físicas ante interrupciones de energía o cierres inesperados de la sesión.
* **Interfaz de Usuario Moderna (Tema Oscuro):** UI estilizada con bordes redondeados, efectos interactivos inteligentes (`Hover`/`Click`) basados en triggers XAML y scrollbars minimalistas para reducir la fatiga visual del operario en jornadas largas.
* **Sistema de Ayuda Contextual Integrado:** Módulos guiados paso a paso y de forma no bloqueante (`Topmost` interactivo) para facilitar la capacitación de nuevo personal directamente mientras operan la app.
* **Persistencia de Red Corporativa:** Soporte para rutas UNC y credenciales de dominio que permiten resguardar los datos conciliados de forma centralizada en servidores locales.
* **Otras Herramientas:** Generador de códigos de barras, módulo de digitalización de soporte e historial de auditorías.

---

## 🛠️ Arquitectura y Tecnologías
La aplicación está desarrollada bajo estrictos estándares de ingeniería de software buscando alta cohesión y bajo acoplamiento:

* **Lenguaje:** C# 10+ / .NET Desktop Application
* **Interfaz Gráfica:** WPF (Windows Presentation Foundation) con estilos XAML globales centralizados en `App.xaml` (brillo automático inteligente adaptable).
* **Procesamiento de Datos:** `ExcelDataReader` para el parseo reactivo de estructuras `.xlsx` sin requerir dependencias de Microsoft Office en las estaciones de trabajo.
* **Patrón de Diseño:** Separación de responsabilidades clara; la lógica de negocio se encuentra orquestada en servicios aislados (`InventarioService`) completamente desacoplados de los elementos visuales de WPF.

---

## ⚙️ Requisitos del Sistema
* **SO:** Windows 10 o superior (64-bit)
* **Framework:** .NET Desktop Runtime 6.0 (o superior)
* **Hardware:** Compatible con cualquier lector de códigos de barras estándar en modo emulación de teclado (Keyboard Wedge).

---

## 🚀 Instalación y Despliegue

### Clonar el repositorio
```bash
git clone [https://github.com/jortizr/OperacionTools.git](https://github.com/jortizr/OperacionTools.git)
cd OperacionTools
