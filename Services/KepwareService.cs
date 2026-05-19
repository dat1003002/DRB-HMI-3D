using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DRB_HMI_3D.Services
{
    public class KepwareService
    {
        private readonly string _endpointUrl = "opc.tcp://192.168.41.30:49320";
        private ApplicationConfiguration _config;
        private Session _session;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        public async Task<object> ReadTagAsync(string nodeId)
        {
            await EnsureConnectedAsync();

            var value = _session.ReadValue(NodeId.Parse(nodeId));

            return value.Value;
        }

        public async Task<Dictionary<string, object>> ReadMultipleTagsAsync(List<string> nodeIds)
        {
            await EnsureConnectedAsync();

            var nodesToRead = new ReadValueIdCollection();

            foreach (var nodeId in nodeIds)
            {
                nodesToRead.Add(new ReadValueId
                {
                    NodeId = NodeId.Parse(nodeId),
                    AttributeId = Attributes.Value
                });
            }

            _session.Read(
                null,
                0,
                TimestampsToReturn.Neither,
                nodesToRead,
                out DataValueCollection results,
                out DiagnosticInfoCollection diagnosticInfos
            );

            ClientBase.ValidateResponse(results, nodesToRead);
            ClientBase.ValidateDiagnosticInfos(diagnosticInfos, nodesToRead);

            var data = new Dictionary<string, object>();

            for (int i = 0; i < nodeIds.Count; i++)
            {
                data[nodeIds[i]] = results[i].Value;
            }

            return data;
        }

        private async Task EnsureConnectedAsync()
        {
            if (_session != null && _session.Connected)
                return;

            await _lock.WaitAsync();

            try
            {
                if (_session != null && _session.Connected)
                    return;

                _config = new ApplicationConfiguration
                {
                    ApplicationName = "DRB_HMI_3D",
                    ApplicationUri = "urn:localhost:DRB_HMI_3D",
                    ApplicationType = ApplicationType.Client,

                    SecurityConfiguration = new SecurityConfiguration
                    {
                        ApplicationCertificate = new CertificateIdentifier
                        {
                            StoreType = "Directory",
                            StorePath = "OPCFoundation/CertificateStores/MachineDefault",
                            SubjectName = "DRB_HMI_3D"
                        },

                        TrustedPeerCertificates = new CertificateTrustList
                        {
                            StoreType = "Directory",
                            StorePath = "OPCFoundation/CertificateStores/UA Applications"
                        },

                        TrustedIssuerCertificates = new CertificateTrustList
                        {
                            StoreType = "Directory",
                            StorePath = "OPCFoundation/CertificateStores/UA Certificate Authorities"
                        },

                        RejectedCertificateStore = new CertificateTrustList
                        {
                            StoreType = "Directory",
                            StorePath = "OPCFoundation/CertificateStores/RejectedCertificates"
                        },

                        AutoAcceptUntrustedCertificates = true,
                        AddAppCertToTrustedStore = true
                    },

                    TransportConfigurations = new TransportConfigurationCollection(),

                    TransportQuotas = new TransportQuotas
                    {
                        OperationTimeout = 15000
                    },

                    ClientConfiguration = new ClientConfiguration
                    {
                        DefaultSessionTimeout = 60000
                    }
                };

                await _config.Validate(ApplicationType.Client);

                _config.CertificateValidator.CertificateValidation += (sender, e) =>
                {
                    e.Accept = true;
                };

                var endpointDescription = CoreClientUtils.SelectEndpoint(
                    _config,
                    _endpointUrl,
                    false
                );

                var endpointConfiguration = EndpointConfiguration.Create(_config);

                var endpoint = new ConfiguredEndpoint(
                    null,
                    endpointDescription,
                    endpointConfiguration
                );

                _session = await Session.Create(
                    _config,
                    endpoint,
                    false,
                    "DRB_HMI_3D_SESSION",
                    60000,
                    null,
                    null
                );
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}