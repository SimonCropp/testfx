﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Composition;

using Analyzer.Utilities;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;

using MSTest.Analyzers.Helpers;

namespace MSTest.Analyzers;

/// <summary>
/// Code fixer for <see cref="PublicTypeShouldBeTestClassAnalyzer"/> and <see cref="TypeContainingTestMethodShouldBeATestClassAnalyzer"/>.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddTestClassFixer))]
[Shared]
public sealed class AddTestClassFixer : CodeFixProvider
{
    /// <inheritdoc />
    public sealed override ImmutableArray<string> FixableDiagnosticIds { get; }
        = ImmutableArray.Create(
            DiagnosticIds.PublicTypeShouldBeTestClassRuleId,
            DiagnosticIds.TypeContainingTestMethodShouldBeATestClassRuleId);

    /// <inheritdoc />
    public override FixAllProvider GetFixAllProvider()
        // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
        => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc />
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        SyntaxNode root = await context.Document.GetRequiredSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        Diagnostic diagnostic = context.Diagnostics[0];
        TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;

        SyntaxToken syntaxToken = root.FindToken(diagnosticSpan.Start);
        if (syntaxToken.Parent is null)
        {
            return;
        }

        // Find the type declaration identified by the diagnostic.
        TypeDeclarationSyntax declaration = syntaxToken.Parent.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().First();

        // Register a code action that will invoke the fix.
        context.RegisterCodeFix(
            CodeAction.Create(
                title: CodeFixResources.AddTestClassFix,
                createChangedDocument: c => AddTestClassAttributeAsync(context.Document, declaration, c),
                equivalenceKey: $"{nameof(AddTestClassFixer)}_{diagnostic.Id}"),
            diagnostic);
    }

    private static async Task<Document> AddTestClassAttributeAsync(Document document, TypeDeclarationSyntax typeDeclaration, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DocumentEditor editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        AttributeSyntax testClassAttribute = SyntaxFactory.Attribute(SyntaxFactory.ParseName("TestClass"));
        AttributeListSyntax attributeList = SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(testClassAttribute));

        TypeDeclarationSyntax newTypeDeclaration = typeDeclaration.AddAttributeLists(attributeList);
        editor.ReplaceNode(typeDeclaration, newTypeDeclaration);

        SyntaxNode newRoot = editor.GetChangedRoot();
        return document.WithSyntaxRoot(newRoot);
    }
}
