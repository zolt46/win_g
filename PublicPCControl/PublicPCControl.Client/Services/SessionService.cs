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

        public Session StartSession(string userName, string userId, string purpose, int minutes)
        {
            var session = new Session
            {
                UserName = userName,
                UserId = userId,
                Purpose = purpose,
                StartTime = DateTime.Now,
                RequestedMinutes = minutes,
                EndReason = string.Empty
            };
            session.Id = _sessionRepository.Insert(session);
            _currentSession = session;
            return session;
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