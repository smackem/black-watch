using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Logging;

namespace BlackWatch.WebApp.Services
{
    public class Navigation : IDisposable
    {
        private readonly NavigationManager _manager;
        private readonly ILogger<Navigation> _logger;
        private readonly LinkedList<string> _history = new();
        private const int MaxHistoryLength = 100;
        private bool _handleLocationChanged = true;

        public Navigation(NavigationManager manager, ILogger<Navigation> logger)
        {
            _manager = manager;
            _logger = logger;
            _manager.LocationChanged += OnLocationChanged;
            _history.AddLast(_manager.Uri);
        }

        public void NavigateTo(string uri) => _manager.NavigateTo(uri);

        public void GoBack()
        {
            if (CanGoBack == false)
            {
                throw new InvalidOperationException("history empty, can't go back");
            }

            _history.RemoveLast(); // pop current location

            _handleLocationChanged = false;
            _manager.NavigateTo(_history.Last!.Value);
            _handleLocationChanged = true;

            _logger.LogInformation("went back. history:\n{NavigationHistory}", string.Join('\n', _history));
        }

        public void Dispose()
        {
            _manager.LocationChanged -= OnLocationChanged;
            GC.SuppressFinalize(this);
        }

        private bool CanGoBack => _history.Count > 1;

        private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            if (_handleLocationChanged == false)
            {
                return;
            }

            _history.AddLast(e.Location);
            while (_history.Count > MaxHistoryLength)
            {
                _history.RemoveFirst();
            }

            _logger.LogInformation("navigated. history:\n{NavigationHistory}", string.Join('\n', _history));
        }
    }
}