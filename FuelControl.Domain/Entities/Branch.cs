namespace FuelControl.Domain.Entities
{
    public sealed class Branch
    {
        public Guid Id { get; private set; }

        public string Name { get; private set; } = null!;
        public long? OmnicommId { get; private set; }
        private Branch()
        {
        }

        public Branch(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException(
                    "Название филиала не может быть пустым.",
                    nameof(name));

            Id = Guid.NewGuid();
            Name = name;
        }

        public void Rename(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException(
                    "Название филиала не может быть пустым.",
                    nameof(name));

            Name = name;
        }
        public void SetOmnicommId(long? omnicommId)
        {
            OmnicommId = omnicommId;
        }
    }
}
