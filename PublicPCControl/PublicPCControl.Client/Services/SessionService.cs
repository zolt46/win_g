// File: PublicPCControl.Client/Services/SessionService.cs
using System;
using PublicPCControl.Client.Data;
using PublicPCControl.Client.Models;

namespace PublicPCControl.Client.Services
{
    public class SessionService
    {
        private readonly SessionRepository _sessionRepository;
        private Session? _currentSession;

        public Session? CurrentSession => _currentSession;

        public SessionService(SessionRepository repository)
        {
            _sessionRepository = repository;
        }

        public Session StartSession(string userName, string userId, string purpose, int minutes, int maxExtensions, int extensionMinutes)
        {
            var session = new Session
            {
                UserName = userName,
                UserId = userId,
                Purpose = purpose,
                StartTime = DateTime.Now,
                RequestedMinutes = minutes,
                MaxExtensions = maxExtensions,
                ExtensionsUsed = 0,
                ExtensionMinutes = extensionMinutes,
                EndReason = string.Empty
            };
            session.Id = _sessionRepository.Insert(session);
            _currentSession = session;
            return session;
        }

        public bool TryExtendSession()
        {
            if (_currentSession == null)
            {
                return false;
            }

            if (_currentSession.ExtensionsUsed >= _currentSession.MaxExtensions || _currentSession.ExtensionMinutes <= 0)
            {
                return false;
            }

            _currentSession.ExtensionsUsed++;
            _currentSession.RequestedMinutes += _currentSession.ExtensionMinutes;
            _sessionRepository.Update(_currentSession);
            return true;
        }

        public void EndSession(string reason)
        {
            if (_currentSession == null) return;
            _currentSession.EndTime = DateTime.Now;
            _currentSession.EndReason = reason;
            _sessionRepository.Update(_currentSession);
            _currentSession = null;
        }
    }
}