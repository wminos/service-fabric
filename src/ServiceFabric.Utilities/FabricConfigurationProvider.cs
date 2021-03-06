﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Fabric;
using System.Fabric.Description;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceFabric.Utilities
{
    public class FabricConfigurationProvider : IConfigurationProvider
    {
        private KeyedCollection<string, ConfigurationProperty> _configurationProperties;

        public FabricConfigurationProvider(string configurationSectionName)
        {
            if (string.IsNullOrWhiteSpace(configurationSectionName))
            {
                throw new ArgumentNullException(nameof(configurationSectionName));
            }

            var activationContext = FabricRuntime.GetActivationContext();
            var configPackage = activationContext.GetConfigurationPackageObject("Config");
            UseConfiguration(configPackage, configurationSectionName);
        }

        public bool HasConfiguration => _configurationProperties != null;

        public string GetValue(string name) => _configurationProperties.ReadConfigurationString(name);
        public string TryGetValue(string name) => _configurationProperties.TryReadConfigurationString(name);

        public bool TryGetValue(string name, out ConfigurationProperty configurationProperty) =>
            _configurationProperties.TryReadConfiguration(name, out configurationProperty);

        private void UseConfiguration(ConfigurationPackage configPackage, string configurationSectionName)
        {
            if (!configPackage.Settings.Sections.Contains(configurationSectionName))
            {
                _configurationProperties = null;
            }
            else
            {
                _configurationProperties = configPackage.Settings.Sections[configurationSectionName].Parameters;
            }
        }

        public bool TryParseInt(string name, out int value)
        {
            var stringValue = _configurationProperties.TryReadConfigurationString(name);
            if (!string.IsNullOrWhiteSpace(stringValue))
            {
                return int.TryParse(stringValue, out value);
            }
            else
            {
                value = 0;
                return false;
            }
        }

        public int ReadIntOrDefault(string name, int defaultValue)
        {
            if (_configurationProperties == null)
            {
                return defaultValue;
            }
            else
            {
                if (!TryParseInt(name, out var value))
                {
                    value = defaultValue;
                }
                return value;
            }
        }

        public async Task<string> RejectConfigurationAsync(string reason, Action<string> onFailure, CancellationToken ct)
        {
            onFailure(reason);
            await Task.Delay(5000, ct).ConfigureAwait(false);
            throw new InvalidOperationException(reason);
        }

        public Task<string> TryReadConfigurationAsync(string name, Action<string> onFailure, CancellationToken ct)
        {
            var value = TryGetValue(name);
            if (string.IsNullOrWhiteSpace(value))
            {
                return RejectConfigurationAsync($"No configuration for {name}", onFailure, ct);
            }
            else
            {
                return Task.FromResult(value);
            }
        }
    }
}
