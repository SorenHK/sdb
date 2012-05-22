namespace SDB.Helpers
{
    class IdGenerator
    {
        private int _nextId;
        private readonly bool _isIncreasing;
        private readonly object _lockObject;

        public IdGenerator(int startValue = 1, bool isIncreasing = true)
        {
            _nextId = startValue;
            _isIncreasing = isIncreasing;

            _lockObject = new object(); 
        }

        public int Get()
        {
            lock(_lockObject)
            {
                var id = _nextId;
                _nextId = _isIncreasing ? _nextId + 1 : _nextId - 1;
                return id;
            }
        }
    }
}
