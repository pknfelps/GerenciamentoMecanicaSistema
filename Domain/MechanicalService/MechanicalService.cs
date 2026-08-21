using Domain.Interface.Exceptions;
using Domain.Interface.Service;

namespace Domain.MechanicalService
{
    public class MechanicalService : IMechanicalService
    {
        public Guid Id { get; private set; }
        public string Description { get; private set; }
        public float Hours { get; private set; }
        public decimal PricePerHour { get; private set; }
        public int Amount { get; private set; }
        public decimal Price => (decimal)Hours * PricePerHour;

        public MechanicalService(string description, float hours, decimal pricePerHour) : this(Guid.NewGuid(), description, hours, pricePerHour, 1) { }

        public MechanicalService(string description, float hours, decimal pricePerHour, int amount) : this(Guid.NewGuid(), description, hours, pricePerHour, amount) { }

        public MechanicalService(Guid id, string description, float hours, decimal pricePerHour, int amount)
        {
            if (id == Guid.Empty)
                throw new DomainValidationException("O ID do serviço não pode ser vazio");

            if (string.IsNullOrEmpty(description))
                throw new DomainValidationException("Descrição do serviço deve ser preenchida");

            if (hours <= 0)
                throw new DomainValidationException("A quantidade de horas não pode ser menor ou igual a zero");

            if (pricePerHour <= 0)
                throw new DomainValidationException("O preço por hora não pode ser menor ou igual a zero");

            if (amount <= 0)
                throw new DomainValidationException("A quantidade não pode ser menor ou igual a zero");

            Id = id;
            Description = description;
            Hours = hours;
            PricePerHour = pricePerHour;
            Amount = amount;
        }

        public void UpdateDescriptrion(string newDescription)
        {
            if (string.IsNullOrEmpty(newDescription))
                throw new DomainValidationException("Descrição do serviço deve ser preenchida");

            Description = newDescription;
        }

        public void UpdateHours(float newHours)
        {
            if (newHours <= 0)
                throw new DomainValidationException("A quantidade de horas do serviço não pode ser menor ou igual a zero");

            Hours = newHours;
        }

        public void UpdatePricePerHour(decimal newPricePerHour)
        {
            if (newPricePerHour <= 0)
                throw new DomainValidationException("O preço por hora do serviço não pode ser menor ou igual a zero");

            PricePerHour = newPricePerHour;
        }

        public void AddServiceAmount(int amount)
        {
            ValidatePositiveAmount(amount);
            Amount += amount;
        }

        public void RemoveServiceAmount(int amount)
        {
            ValidatePositiveAmount(amount);

            if (amount > Amount)
                throw new DomainBusinessRuleException("Não é possível remover uma quantidade de serviços maior que a existente na ordem");

            Amount -= amount;
        }

        private static void ValidatePositiveAmount(int amount)
        {
            if (amount <= 0)
                throw new DomainValidationException("Quantidade deve ser maior que zero");
        }
    }
}
