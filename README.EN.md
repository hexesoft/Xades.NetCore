Xades.NetCore
=============

 
INTRODUCTION
-------------
FirmaXadesNet is a C# (.NET 5.0) library for creation of XAdES signatures. Developed by the Department of New Technologies of the Urban Planning Department of the Cartagena City Council, it is based on a modification of the XAdES starter kit developed by Microsoft France.


FEATURES
--------

- Creation of XAdES-BES, XAdES-EPES, XAdES-T y XAdES-XL signatures.

- Use of Windows-Supported certificates, Smart Cards, Usb Tokens....

- Signature Formats: Externally Detached, Internally Detached, Enveloped y Enveloping.

- Certificte validation by OCSP or against revocation list.

- Support for co-signatures and countersignatures.

- Support for RSA-SHA1, RSA-SHA256 y RSA-SHA512.

Within the solution there is a project with examples of use of the library. Some of the examples make use of the ACCV timestamp server (Agencia de Tecnología y Certificación Electrónica, Spain).

As an example of use, start the TextFirmaXades project, which allows  to digitally sign Xml files.

**Enveloped Signature Example:**

```C#
    var xadesService = new XadesService();
    var signatureParameters = new SignatureParameters 
    {
        SignatureMethod = SignatureMethod.RSAwithSHA512,
        SigningDate = DateTime.Now,
        SignaturePackaging = SignaturePackaging.ENVELOPED,
    };

    // Test SignatureCommitment
    var signatureCommitment = new SignatureCommitment(SignatureCommitmentType.ProofOfOrigin);
    signatureParameters.SignatureCommitments.Add(sc);    

    // Note: Use "CertUtil.SelectCertificate()" to choose certificate
    var signingCertificate = new X509Certificate2(....);
    using (signatureParameters.Signer = new Signer(signingCertificate))
    {
        using var fileStream = new FileStream(xmlFilePath, FileMode.Open);        
        var signedXmlDocument = xadesService.Sign(fileStream, signatureParameters);        

        // Store
        // signedDocument.Save(signedXmlFilePath);

        // ToString
        // string signedXml = signedDocument.Document.OuterXml;
    }        
```
