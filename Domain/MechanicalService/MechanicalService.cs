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
                throw new DomainValidationException("Id do serviço não pode ser vazio");

            if (string.IsNullOrEmpty(description))
                throw new DomainValidationException("Descrição do serviço deve ser preenchida");

            if (hours <= 0)
                throw new DomainValidationException("Quantidade de horas não pode ser menor ou igual a 0");

            if (pricePerHour <= 0)
                throw new DomainValidationException("Preço por hora não pode ser menor ou igual a 0");

            if (amount <= 0)
                throw new DomainValidationException("Quantidade não pode ser menor ou igual a 0.");

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
                throw new DomainValidationException("Horas do serviço não pode ser menor ou igual a 0");

            Hours = newHours;
        }

        public void UpdatePricePerHour(decimal newPricePerHour)
        {
            if (newPricePerHour <= 0)
                throw new DomainValidationException("Preço por hora do serviço não pode ser menor ou igual a 0");

            PricePerHour = newPricePerHour;
        }

        public void AddServiceAmount(int amount) => Amount += amount;

        public void RemoveServiceAmount(int amount)
        {
            if (amount > Amount)
                throw new DomainBusinessRuleException("Não é possível remover mais serviços do que há na orodem");

            Amount -= amount;
        }
    }
}