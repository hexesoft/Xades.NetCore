Xades.NetCore
=============

[ENGLISH](/README.EN.md)
[ITALIANO](/README.IT.md)

INTRODUCCIÓN
-------------
FirmaXadesNet es una librería desarrollada en C# (.NET 5.0) para la generación de firmas XAdES realizada por el Dpto. de Nuevas Tecnologías de la Concejalía de Urbanismo del Ayuntamiento de Cartagena, la cual está basada en una modificación del XAdES starter kit desarrollado por Microsoft Francia.


CARACTERÍSTICAS
---------------

- Generación de firmas XAdES-BES, XAdES-EPES, XAdES-T y XAdES-XL.

- Utiliza los certificados admitidos por Windows, incluido Smart Cards, Token Usb....

- Formatos Externally Detached, Internally Detached, Enveloped y Enveloping.

- Validación de certificados mediante OCSP y listas de revocación.

- Soporte para co-firmas y contrafirmas.

- Soporte para métodos de firma RSA-SHA1, RSA-SHA256 y RSA-SHA512.

Dentro de la solución se encuentra un proyecto con ejemplos de uso de la librería. Algunos de los ejemplos hacen uso del servidor de sellado de tiempo de la ACCV.

Como ejemplo, inicie el proyecto TextFirmaXades, que permite firmar digitalmente archivos Xml.
