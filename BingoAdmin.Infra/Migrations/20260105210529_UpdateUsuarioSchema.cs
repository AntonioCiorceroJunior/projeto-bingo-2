using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BingoAdmin.Infra.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUsuarioSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Bairro",
                table: "Usuarios",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Cep",
                table: "Usuarios",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Cidade",
                table: "Usuarios",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Complemento",
                table: "Usuarios",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Cpf",
                table: "Usuarios",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DataValidadeLicenca",
                table: "Usuarios",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Estado",
                table: "Usuarios",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsAdmin",
                table: "Usuarios",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsLogado",
                table: "Usuarios",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Logradouro",
                table: "Usuarios",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Numero",
                table: "Usuarios",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StatusAssinatura",
                table: "Usuarios",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Telefone",
                table: "Usuarios",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimoAcesso",
                table: "Usuarios",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Bairro",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "Cep",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "Cidade",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "Complemento",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "Cpf",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "DataValidadeLicenca",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "Estado",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "IsAdmin",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "IsLogado",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "Logradouro",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "Numero",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "StatusAssinatura",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "Telefone",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "UltimoAcesso",
                table: "Usuarios");
        }
    }
}
