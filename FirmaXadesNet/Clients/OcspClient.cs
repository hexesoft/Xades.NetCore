﻿// --------------------------------------------------------------------------------------------------------------------
// OcspClient.cs
//
// FirmaXadesNet - Librería para la generación de firmas XADES
// Copyright (C) 2016 Dpto. de Nuevas Tecnologías de la Dirección General de Urbanismo del Ayto. de Cartagena
//
// This program is free software: you can redistribute it and/or modify
// it under the +terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with this program.  If not, see http://www.gnu.org/licenses/. 
//
// E-Mail: informatica@gemuc.es
// 
// --------------------------------------------------------------------------------------------------------------------

using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Ocsp;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Ocsp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Xades.NetCore.Utils;
using BouncyCastleX509Certificate = Org.BouncyCastle.X509.X509Certificate;
using BouncyCastleX509Extension = Org.BouncyCastle.Asn1.X509.X509Extension;

namespace Xades.NetCore.Clients
{
    public enum CertificateStatus { Good = 0, Revoked = 1, Unknown = 2 };

    public class OcspClient
    {
        #region Private variables

        private Asn1OctetString _nonceAsn1OctetString;

        #endregion

        #region Public methods

        /// <summary>
        /// Método que comprueba el estado de un certificado
        /// </summary>
        /// <param name="eeCert"></param>
        /// <param name="issuerCert"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public byte[] QueryBinary(BouncyCastleX509Certificate eeCert, BouncyCastleX509Certificate issuerCert, string url, GeneralName requestorName = null,
            X509Certificate2 signCertificate = null)
        {
            OcspReq req = GenerateOcspRequest(issuerCert, eeCert.SerialNumber, requestorName, signCertificate);

            byte[] binaryResp = PostData(url, req.GetEncoded(), "application/ocsp-request", "application/ocsp-response");

            return binaryResp;
        }

        /// <summary>
        /// Devuelve la URL del servidor OCSP que contenga el certificado
        /// </summary>
        /// <param name="cert"></param>
        /// <returns></returns>
        public string GetAuthorityInformationAccessOcspUrl(BouncyCastleX509Certificate cert)
        {
            List<string> ocspUrls = new List<string>();

            try
            {
                Asn1Object obj = GetExtensionValue(cert, X509Extensions.AuthorityInfoAccess.Id);

                if (obj == null)
                {
                    return null;
                }

                // Switched to manual parse 
                Asn1Sequence s = (Asn1Sequence)obj;
                IEnumerator elements = s.GetEnumerator();

                while (elements.MoveNext())
                {
                    Asn1Sequence element = (Asn1Sequence)elements.Current;
                    DerObjectIdentifier oid = (DerObjectIdentifier)element[0];

                    if (oid.Id.Equals("1.3.6.1.5.5.7.48.1")) // Is Ocsp? 
                    {
                        Asn1TaggedObject taggedObject = (Asn1TaggedObject)element[1];
                        GeneralName gn = GeneralName.GetInstance(taggedObject);
                        ocspUrls.Add(DerIA5String.GetInstance(gn.Name).GetString());
                    }
                }
            }
            catch
            {
                return null;
            }

            return ocspUrls[0];
        }

        /// <summary>
        /// Procesa la respuesta del servidor OCSP y devuelve el estado del certificado
        /// </summary>
        /// <param name="binaryResp"></param>
        /// <returns></returns>
        public CertificateStatus ProcessOcspResponse(byte[] binaryResp)
        {
            if (binaryResp.Length == 0)
            {
                return CertificateStatus.Unknown;
            }

            OcspResp r = new OcspResp(binaryResp);
            CertificateStatus cStatus = CertificateStatus.Unknown;

            if (r.Status == OcspRespStatus.Successful)
            {
                BasicOcspResp or = (BasicOcspResp)r.GetResponseObject();

                if (or.GetExtensionValue(OcspObjectIdentifiers.PkixOcspNonce).ToString() !=
                    _nonceAsn1OctetString.ToString())
                {
                    throw new Exception("Bad nonce value");
                }

                if (or.Responses.Length == 1)
                {
                    SingleResp resp = or.Responses[0];

                    object certificateStatus = resp.GetCertStatus();

                    if (certificateStatus == Org.BouncyCastle.Ocsp.CertificateStatus.Good)
                    {
                        cStatus = CertificateStatus.Good;
                    }
                    else if (certificateStatus is RevokedStatus)
                    {
                        cStatus = CertificateStatus.Revoked;
                    }
                    else if (certificateStatus is UnknownStatus)
                    {
                        cStatus = CertificateStatus.Unknown;
                    }
                }

            }
            else
            {
                throw new Exception("Unknow status '" + r.Status + "'.");
            }

            return cStatus;
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Construye la petición web y devuelve el resultado de la misma
        /// </summary>
        /// <param name="url"></param>
        /// <param name="data"></param>
        /// <param name="contentType"></param>
        /// <param name="accept"></param>
        /// <returns></returns>
        private byte[] PostData(string url, byte[] data, string contentType, string accept)
        {
            byte[] resp;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = contentType;
            request.ContentLength = data.Length;
            request.Accept = accept;

            Stream stream = request.GetRequestStream();
            stream.Write(data, 0, data.Length);
            stream.Close();
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream respStream = response.GetResponseStream();
            using (MemoryStream ms = new())
            {
                respStream.CopyTo(ms);
                resp = ms.ToArray();
                respStream.Close();
            }

            return resp;
        }


        protected static Asn1Object GetExtensionValue(BouncyCastleX509Certificate cert,
                string oid)
        {
            if (cert == null)
            {
                return null;
            }

            byte[] bytes = cert.GetExtensionValue(new DerObjectIdentifier(oid)).GetOctets();

            if (bytes == null)
            {
                return null;
            }

            Asn1InputStream aIn = new Asn1InputStream(bytes);

            return aIn.ReadObject();
        }


        private OcspReq GenerateOcspRequest(BouncyCastleX509Certificate issuerCert, BigInteger serialNumber, GeneralName requestorName,
            X509Certificate2 signCertificate)
        {
            CertificateID id = new CertificateID(CertificateID.HashSha1, issuerCert, serialNumber);
            return GenerateOcspRequest(id, requestorName, signCertificate);
        }

        private OcspReq GenerateOcspRequest(CertificateID id, GeneralName requestorName,
            X509Certificate2 signCertificate)
        {
            OcspReqGenerator ocspRequestGenerator = new OcspReqGenerator();

            ocspRequestGenerator.AddRequest(id);

            if (requestorName != null)
            {
                ocspRequestGenerator.SetRequestorName(requestorName);
            }


            _nonceAsn1OctetString = new DerOctetString(new DerOctetString(BigInteger.ValueOf(DateTime.Now.Ticks).ToByteArray()));
            var oids = new List<DerObjectIdentifier>
            {
                OcspObjectIdentifiers.PkixOcspNonce,
            };
            var values = new Dictionary<DerObjectIdentifier, BouncyCastleX509Extension>
            {
                [OcspObjectIdentifiers.PkixOcspNonce] = new BouncyCastleX509Extension(false, _nonceAsn1OctetString),
            };
            ocspRequestGenerator.SetRequestExtensions(new X509Extensions(oids, values));

            if (signCertificate != null)
            {
                return ocspRequestGenerator.Generate((RSACryptoServiceProvider)signCertificate.GetRSAPrivateKey(), CertUtil.GetCertChain(signCertificate));
            }
            else
            {
                return ocspRequestGenerator.Generate();
            }
        }

        #endregion
    }
}
