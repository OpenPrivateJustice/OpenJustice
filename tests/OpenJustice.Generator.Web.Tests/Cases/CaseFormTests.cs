using OpenJustice.Generator.Web.Models.Cases;
using OpenJustice.Generator.Infrastructure.Persistence.Entities;
using FluentAssertions;

namespace OpenJustice.Generator.Web.Tests.Cases;

public class CaseFormTests
{
    [Fact]
    public void ToCreateRequest_WithValidModel_ReturnsRequestWithAllFields()
    {
        // Arrange
        var form = new CaseFormModel
        {
            CrimeDate = new DateTime(2024, 1, 15),
            ReportDate = new DateTime(2024, 2, 1),
            VictimName = "João Silva",
            VictimGender = "M",
            VictimAge = 35,
            VictimNationality = "Brasileiro",
            VictimProfession = "Professor",
            VictimRelationshipToAccused = "Desconhecido",
            VictimConfidence = 80,
            AccusedName = "José Santos",
            AccusedGender = "M",
            AccusedAge = 40,
            AccusedNationality = "Brasileiro",
            AccusedProfession = "Motorista",
            AccusedDocument = "12345678900",
            AccusedAddress = "Rua X, 123",
            AccusedRelationshipToVictim = "Vizinho",
            AccusedConfidence = 75,
            CrimeTypeId = 1,
            CrimeSubtype = "Doloso",
            EstimatedCrimeDateTime = new DateTime(2024, 1, 15, 14, 30, 0),
            CrimeLocationAddress = "Avenida Principal, 100",
            CrimeLocationCity = "São Paulo",
            CrimeLocationState = "SP",
            CrimeCoordinates = "-23.5505,-46.6333",
            CrimeDescription = "Descrição do crime",
            CaseTypeId = 2,
            NumberOfVictims = 1,
            NumberOfAccused = 1,
            WeaponUsed = "Faca",
            Motivation = "Roubo",
            Premeditation = "Sim",
            CrimeConfidence = 90,
            JudicialStatusId = 3,
            ProcessNumber = "1234567-89.2024.8.26.0053",
            Court = "Forum Central",
            County = "São Paulo",
            CurrentPhase = "Instrução",
            JudicialReportDate = new DateTime(2024, 2, 15),
            SentencingDate = null,
            Sentence = null,
            PendingAppeals = null,
            JudicialConfidence = 85,
            MainCategory = "Homicídio",
            IsSensitiveContent = true,
            IsVerified = false,
            AnonymizationStatus = "Parcial",
            CuratorId = "curator1"
        };

        // Act
        var request = form.ToCreateRequest();

        // Assert
        request.CrimeDate.Should().Be(form.CrimeDate);
        request.ReportDate.Should().Be(form.ReportDate);
        request.VictimName.Should().Be(form.VictimName);
        request.VictimGender.Should().Be(form.VictimGender);
        request.VictimAge.Should().Be(form.VictimAge);
        request.VictimNationality.Should().Be(form.VictimNationality);
        request.VictimProfession.Should().Be(form.VictimProfession);
        request.VictimRelationshipToAccused.Should().Be(form.VictimRelationshipToAccused);
        request.VictimConfidence.Should().Be(form.VictimConfidence);
        request.AccusedName.Should().Be(form.AccusedName);
        request.AccusedGender.Should().Be(form.AccusedGender);
        request.AccusedAge.Should().Be(form.AccusedAge);
        request.AccusedNationality.Should().Be(form.AccusedNationality);
        request.AccusedProfession.Should().Be(form.AccusedProfession);
        request.AccusedDocument.Should().Be(form.AccusedDocument);
        request.AccusedAddress.Should().Be(form.AccusedAddress);
        request.AccusedRelationshipToVictim.Should().Be(form.AccusedRelationshipToVictim);
        request.AccusedConfidence.Should().Be(form.AccusedConfidence);
        request.CrimeTypeId.Should().Be(form.CrimeTypeId);
        request.CrimeSubtype.Should().Be(form.CrimeSubtype);
        request.EstimatedCrimeDateTime.Should().Be(form.EstimatedCrimeDateTime);
        request.CrimeLocationAddress.Should().Be(form.CrimeLocationAddress);
        request.CrimeLocationCity.Should().Be(form.CrimeLocationCity);
        request.CrimeLocationState.Should().Be(form.CrimeLocationState);
        request.CrimeCoordinates.Should().Be(form.CrimeCoordinates);
        request.CrimeDescription.Should().Be(form.CrimeDescription);
        request.CaseTypeId.Should().Be(form.CaseTypeId);
        request.NumberOfVictims.Should().Be(form.NumberOfVictims);
        request.NumberOfAccused.Should().Be(form.NumberOfAccused);
        request.WeaponUsed.Should().Be(form.WeaponUsed);
        request.Motivation.Should().Be(form.Motivation);
        request.Premeditation.Should().Be(form.Premeditation);
        request.CrimeConfidence.Should().Be(form.CrimeConfidence);
        request.JudicialStatusId.Should().Be(form.JudicialStatusId);
        request.ProcessNumber.Should().Be(form.ProcessNumber);
        request.Court.Should().Be(form.Court);
        request.County.Should().Be(form.County);
        request.CurrentPhase.Should().Be(form.CurrentPhase);
        request.JudicialReportDate.Should().Be(form.JudicialReportDate);
        request.SentencingDate.Should().Be(form.SentencingDate);
        request.Sentence.Should().Be(form.Sentence);
        request.PendingAppeals.Should().Be(form.PendingAppeals);
        request.JudicialConfidence.Should().Be(form.JudicialConfidence);
        request.MainCategory.Should().Be(form.MainCategory);
        request.IsSensitiveContent.Should().Be(form.IsSensitiveContent);
        request.IsVerified.Should().Be(form.IsVerified);
        request.AnonymizationStatus.Should().Be(form.AnonymizationStatus);
        request.CuratorId.Should().Be(form.CuratorId);
    }

    [Fact]
    public void ToUpdateRequest_WithValidModel_ReturnsRequestWithAllFields()
    {
        // Arrange
        var form = new CaseFormModel
        {
            CrimeTypeId = 1,
            CaseTypeId = 2,
            JudicialStatusId = 3
        };

        // Act
        var request = form.ToUpdateRequest();

        // Assert
        request.CrimeTypeId.Should().Be(1);
        request.CaseTypeId.Should().Be(2);
        request.JudicialStatusId.Should().Be(3);
    }

    [Fact]
    public void FromCase_WithValidEntity_ReturnsFormModelWithAllFields()
    {
        // Arrange
        var entity = new Case
        {
            Id = 42,
            ReferenceCode = "ATRO-2026-0001",
            RegistrationDate = new DateTime(2024, 1, 1),
            CrimeDate = new DateTime(2024, 1, 15),
            ReportDate = new DateTime(2024, 2, 1),
            LastUpdated = new DateTime(2024, 2, 15),
            VictimName = "Maria Santos",
            VictimGender = "F",
            VictimAge = 28,
            VictimNationality = "Brasileira",
            VictimProfession = "Enfermeira",
            VictimRelationshipToAccused = "Ex-namorado",
            VictimConfidence = 85,
            AccusedName = "Pedro Oliveira",
            AccusedGender = "M",
            AccusedAge = 32,
            AccusedNationality = "Brasileiro",
            AccusedProfession = "Técnico de Informática",
            AccusedDocument = "98765432100",
            AccusedAddress = "Rua Y, 456",
            AccusedRelationshipToVictim = "Ex-namorado",
            AccusedConfidence = 80,
            CrimeTypeId = 1,
            CrimeSubtype = "Feminicídio",
            EstimatedCrimeDateTime = new DateTime(2024, 1, 15, 22, 0, 0),
            CrimeLocationAddress = "Rua das Flores, 78",
            CrimeLocationCity = "Rio de Janeiro",
            CrimeLocationState = "RJ",
            CrimeCoordinates = "-22.9068,-43.1729",
            CrimeDescription = "Vítima foi encontrada sem vida",
            CaseTypeId = 2,
            NumberOfVictims = 1,
            NumberOfAccused = 1,
            WeaponUsed = "Arma branca",
            Motivation = "Ciúmes",
            Premeditation = "Sim",
            CrimeConfidence = 95,
            JudicialStatusId = 5,
            ProcessNumber = "9876543-21.2024.1.1.001",
            Court = "Tribunal de Justiça",
            County = "Rio de Janeiro",
            CurrentPhase = "Julgamento",
            JudicialReportDate = new DateTime(2024, 3, 1),
            SentencingDate = new DateTime(2024, 6, 15),
            Sentence = "Prisão perpétua",
            PendingAppeals = "Recurso extraordinário",
            JudicialConfidence = 90,
            MainCategory = "Feminicídio",
            IsSensitiveContent = true,
            IsVerified = true,
            AnonymizationStatus = "Nenhum",
            CurationStatus = Domain.Enums.CurationStatus.Approved,
            CurationTimestamp = new DateTime(2024, 1, 5),
            CreatedAt = new DateTime(2024, 1, 1),
            UpdatedAt = new DateTime(2024, 6, 15),
            CuratorId = "curator1"
        };

        // Act
        var form = CaseFormModel.FromCase(entity);

        // Assert
        form.CrimeDate.Should().Be(entity.CrimeDate);
        form.ReportDate.Should().Be(entity.ReportDate);
        form.VictimName.Should().Be(entity.VictimName);
        form.VictimGender.Should().Be(entity.VictimGender);
        form.VictimAge.Should().Be(entity.VictimAge);
        form.VictimNationality.Should().Be(entity.VictimNationality);
        form.VictimProfession.Should().Be(entity.VictimProfession);
        form.VictimRelationshipToAccused.Should().Be(entity.VictimRelationshipToAccused);
        form.VictimConfidence.Should().Be(entity.VictimConfidence);
        form.AccusedName.Should().Be(entity.AccusedName);
        form.AccusedGender.Should().Be(entity.AccusedGender);
        form.AccusedAge.Should().Be(entity.AccusedAge);
        form.AccusedNationality.Should().Be(entity.AccusedNationality);
        form.AccusedProfession.Should().Be(entity.AccusedProfession);
        form.AccusedDocument.Should().Be(entity.AccusedDocument);
        form.AccusedAddress.Should().Be(entity.AccusedAddress);
        form.AccusedRelationshipToVictim.Should().Be(entity.AccusedRelationshipToVictim);
        form.AccusedConfidence.Should().Be(entity.AccusedConfidence);
        form.CrimeTypeId.Should().Be(entity.CrimeTypeId);
        form.CrimeSubtype.Should().Be(entity.CrimeSubtype);
        form.EstimatedCrimeDateTime.Should().Be(entity.EstimatedCrimeDateTime);
        form.CrimeLocationAddress.Should().Be(entity.CrimeLocationAddress);
        form.CrimeLocationCity.Should().Be(entity.CrimeLocationCity);
        form.CrimeLocationState.Should().Be(entity.CrimeLocationState);
        form.CrimeCoordinates.Should().Be(entity.CrimeCoordinates);
        form.CrimeDescription.Should().Be(entity.CrimeDescription);
        form.CaseTypeId.Should().Be(entity.CaseTypeId);
        form.NumberOfVictims.Should().Be(entity.NumberOfVictims);
        form.NumberOfAccused.Should().Be(entity.NumberOfAccused);
        form.WeaponUsed.Should().Be(entity.WeaponUsed);
        form.Motivation.Should().Be(entity.Motivation);
        form.Premeditation.Should().Be(entity.Premeditation);
        form.CrimeConfidence.Should().Be(entity.CrimeConfidence);
        form.JudicialStatusId.Should().Be(entity.JudicialStatusId);
        form.ProcessNumber.Should().Be(entity.ProcessNumber);
        form.Court.Should().Be(entity.Court);
        form.County.Should().Be(entity.County);
        form.CurrentPhase.Should().Be(entity.CurrentPhase);
        form.JudicialReportDate.Should().Be(entity.JudicialReportDate);
        form.SentencingDate.Should().Be(entity.SentencingDate);
        form.Sentence.Should().Be(entity.Sentence);
        form.PendingAppeals.Should().Be(entity.PendingAppeals);
        form.JudicialConfidence.Should().Be(entity.JudicialConfidence);
        form.MainCategory.Should().Be(entity.MainCategory);
        form.IsSensitiveContent.Should().Be(entity.IsSensitiveContent);
        form.IsVerified.Should().Be(entity.IsVerified);
        form.AnonymizationStatus.Should().Be(entity.AnonymizationStatus);
        form.CuratorId.Should().Be(entity.CuratorId);
    }

    [Fact]
    public void FromCase_WithNullFields_ReturnsFormModelWithNulls()
    {
        // Arrange
        var entity = new Case
        {
            Id = 1,
            ReferenceCode = "ATRO-2026-0001",
            RegistrationDate = new DateTime(2024, 1, 1),
            CrimeDate = null,
            ReportDate = null,
            LastUpdated = new DateTime(2024, 1, 1),
            VictimName = null,
            VictimGender = null,
            VictimAge = null,
            VictimNationality = null,
            VictimProfession = null,
            VictimRelationshipToAccused = null,
            VictimConfidence = 50,
            AccusedName = null,
            AccusedGender = null,
            AccusedAge = null,
            AccusedNationality = null,
            AccusedProfession = null,
            AccusedDocument = null,
            AccusedAddress = null,
            AccusedRelationshipToVictim = null,
            AccusedConfidence = 50,
            CrimeTypeId = 1,
            CrimeSubtype = null,
            EstimatedCrimeDateTime = null,
            CrimeLocationAddress = null,
            CrimeLocationCity = null,
            CrimeLocationState = null,
            CrimeCoordinates = null,
            CrimeDescription = null,
            CaseTypeId = 1,
            NumberOfVictims = 1,
            NumberOfAccused = 1,
            WeaponUsed = null,
            Motivation = null,
            Premeditation = null,
            CrimeConfidence = 50,
            JudicialStatusId = 1,
            ProcessNumber = null,
            Court = null,
            County = null,
            CurrentPhase = null,
            JudicialReportDate = null,
            SentencingDate = null,
            Sentence = null,
            PendingAppeals = null,
            JudicialConfidence = 50,
            MainCategory = null,
            IsSensitiveContent = false,
            IsVerified = false,
            AnonymizationStatus = null,
            CurationStatus = Domain.Enums.CurationStatus.Pending,
            CurationTimestamp = null,
            CreatedAt = new DateTime(2024, 1, 1),
            UpdatedAt = new DateTime(2024, 1, 1),
            CuratorId = null
        };

        // Act
        var form = CaseFormModel.FromCase(entity);

        // Assert
        form.CrimeDate.Should().BeNull();
        form.ReportDate.Should().BeNull();
        form.VictimName.Should().BeNull();
        form.VictimGender.Should().BeNull();
        form.VictimAge.Should().BeNull();
        form.CrimeSubtype.Should().BeNull();
        form.ProcessNumber.Should().BeNull();
        form.Court.Should().BeNull();
    }

    [Fact]
    public void DefaultValues_AreSetCorrectly()
    {
        // Act
        var form = new CaseFormModel();

        // Assert
        form.VictimConfidence.Should().Be(50);
        form.AccusedConfidence.Should().Be(50);
        form.CrimeConfidence.Should().Be(50);
        form.JudicialConfidence.Should().Be(50);
        form.NumberOfVictims.Should().Be(1);
        form.NumberOfAccused.Should().Be(1);
        form.IsSensitiveContent.Should().BeFalse();
        form.IsVerified.Should().BeFalse();
        form.CrimeTypeId.Should().Be(0);
        form.CaseTypeId.Should().Be(0);
        form.JudicialStatusId.Should().Be(0);
    }
}
