namespace Sample.AspNetCore20
{
    public class GreetingService
    {
        private readonly bool _isVisitorWelcome;

        public GreetingService(bool isVisitorWelcome)
        {
            _isVisitorWelcome = isVisitorWelcome;
        }

        public string Greet()
        {
            if (_isVisitorWelcome)
            {
                return "WILL YOU GO AWAY ALREADY!!";
            }
            else
            {
                return "Pleased to meet you.";
            }
        }

    }
}
