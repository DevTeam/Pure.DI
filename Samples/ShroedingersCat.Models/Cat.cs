namespace ShroedingersCat.Models
{
    public class Cat : ICat
    {
        private readonly string _name;

        public Cat(string name) => _name = name;

        public override string ToString() => _name;
    }
}