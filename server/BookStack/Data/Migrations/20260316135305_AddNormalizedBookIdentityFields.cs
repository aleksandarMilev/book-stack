using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookStack.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNormalizedBookIdentityFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NormalizedAuthor",
                table: "Books",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedIsbn",
                table: "Books",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedTitle",
                table: "Books",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE [Books]
                SET
                    [NormalizedTitle] = LTRIM(RTRIM(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
                        TRANSLATE(
                            REPLACE(REPLACE(REPLACE(UPPER(LTRIM(RTRIM(ISNULL([Title], N'')))), CHAR(9), N' '), CHAR(10), N' '), CHAR(13), N' '),
                            N'!"#$%&''()*+,-./:;<=>?@[\]^_`{|}~',
                            REPLICATE(N' ', LEN(N'!"#$%&''()*+,-./:;<=>?@[\]^_`{|}~'))
                        ),
                        N'  ', N' '),
                        N'  ', N' '),
                        N'  ', N' '),
                        N'  ', N' '),
                        N'  ', N' '),
                        N'  ', N' '))),
                    [NormalizedAuthor] = LTRIM(RTRIM(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
                        TRANSLATE(
                            REPLACE(REPLACE(REPLACE(UPPER(LTRIM(RTRIM(ISNULL([Author], N'')))), CHAR(9), N' '), CHAR(10), N' '), CHAR(13), N' '),
                            N'!"#$%&''()*+,-./:;<=>?@[\]^_`{|}~',
                            REPLICATE(N' ', LEN(N'!"#$%&''()*+,-./:;<=>?@[\]^_`{|}~'))
                        ),
                        N'  ', N' '),
                        N'  ', N' '),
                        N'  ', N' '),
                        N'  ', N' '),
                        N'  ', N' '),
                        N'  ', N' '))),
                    [NormalizedIsbn] = NULLIF(
                        UPPER(
                            REPLACE(
                                REPLACE(
                                    LTRIM(RTRIM(ISNULL([Isbn], N''))),
                                    N' ',
                                    N''),
                                N'-',
                                N'')
                        ),
                        N''
                    );
                """);

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedAuthor",
                table: "Books",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedTitle",
                table: "Books",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(300)",
                oldMaxLength: 300,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Books_NormalizedIsbn",
                table: "Books",
                column: "NormalizedIsbn",
                unique: true,
                filter: "[NormalizedIsbn] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Books_NormalizedTitle_NormalizedAuthor",
                table: "Books",
                columns: new[] { "NormalizedTitle", "NormalizedAuthor" },
                unique: true,
                filter: "[NormalizedIsbn] IS NULL AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Books_NormalizedIsbn",
                table: "Books");

            migrationBuilder.DropIndex(
                name: "IX_Books_NormalizedTitle_NormalizedAuthor",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "NormalizedAuthor",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "NormalizedIsbn",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "NormalizedTitle",
                table: "Books");
        }
    }
}
