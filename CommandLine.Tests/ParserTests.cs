﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using FluentAssertions;
using System.Linq;
using Xunit;
using static Microsoft.DotNet.Cli.CommandLine.Accept;
using static Microsoft.DotNet.Cli.CommandLine.Create;

namespace Microsoft.DotNet.Cli.CommandLine.Tests
{
    public class ParserTests
    {
        [Fact]
        public void An_option_without_a_long_form_can_be_checked_for_using_a_prefix()
        {
            var result = new Parser(
                    Option("--flag", ""))
                .Parse("--flag");

            result.HasOption("--flag").Should().BeTrue();
        }

        [Fact]
        public void An_option_without_a_long_form_can_be_checked_for_without_using_a_prefix()
        {
            var result = new Parser(
                    Option("--flag", ""))
                .Parse("--flag");

            result.HasOption("flag").Should().BeTrue();
        }

        [Fact]
        public void When_invoked_by_its_short_form_an_option_with_an_alias_can_be_checked_for_by_its_short_form()
        {
            var result = new Parser(
                    Option("-o|--one", ""))
                .Parse("-o");

            result.HasOption("o").Should().BeTrue();
        }

        [Fact]
        public void When_invoked_by_its_long_form_an_option_with_an_alias_can_be_checked_for_by_its_short_form()
        {
            var result = new Parser(
                    Option("-o|--one", ""))
                .Parse("--one");

            result.HasOption("o").Should().BeTrue();
        }

        [Fact]
        public void When_invoked_by_its_short_form_an_option_with_an_alias_can_be_checked_for_by_its_long_form()
        {
            var result = new Parser(
                    Option("-o|--one", ""))
                .Parse("-o");

            result.HasOption("one").Should().BeTrue();
        }

        [Fact]
        public void When_invoked_by_its_long_form_an_option_with_an_alias_can_be_checked_for_by_its_long_form()
        {
            var result = new Parser(
                    Option("-o|--one", ""))
                .Parse("--one");

            result.HasOption("one").Should().BeTrue();
        }

        [Fact]
        public void A_short_form_option_can_be_invoked_using_a_slash_prefix()
        {
            var result = new Parser(
                    Option("-o|--one", "", ZeroOrMoreArguments))
                .Parse("/o");

            result.HasOption("one").Should().BeTrue();
        }

        [Fact]
        public void A_long_form_option_can_be_invoked_using_a_slash_prefix()
        {
            var result = new Parser(
                    Option("-o|--one", "", ZeroOrMoreArguments))
                .Parse("/one");

            result.HasOption("one").Should().BeTrue();
        }

        [Fact]
        public void Two_options_are_parsed_correctly()
        {
            var result = new Parser(
                    Option("-o|--one", ""),
                    Option("-t|--two", ""))
                .Parse("-o -t");

            result.HasOption("o").Should().BeTrue();
            result.HasOption("one").Should().BeTrue();
            result.HasOption("t").Should().BeTrue();
            result.HasOption("two").Should().BeTrue();
        }

        [Fact]
        public void Parse_result_contains_arguments_to_options()
        {
            var result = new Parser(
                    Option("-o|--one", "", ExactlyOneArgument),
                    Option("-t|--two", "", ExactlyOneArgument))
                .Parse("-o args_for_one -t args_for_two");

            result["one"].Arguments.Single().Should().Be("args_for_one");
            result["two"].Arguments.Single().Should().Be("args_for_two");
        }

        [Fact]
        public void When_no_options_are_specified_then_an_error_is_returned()
        {
            var result = new Parser().Parse("-x");

            result.Errors
                  .Single()
                  .Message
                  .Should()
                  .Be("Option '-x' is not recognized.");
        }

        [Fact]
        public void Two_options_cannot_have_conflicting_aliases()
        {
            Action create = () =>
                new Parser(
                    Option("-o|--one", ""),
                    Option("-t|--one", ""));

            create.ShouldThrow<ArgumentException>()
                  .Which
                  .Message
                  .Should()
                  .Be("Alias 'one' is already in use.");
        }

        [Fact]
        public void A_double_dash_delimiter_specifies_that_no_further_command_line_args_will_be_treated_options()
        {
            var result = new Parser(
                    Option("-o|--one", ""))
                .Parse("-o \"some stuff\" -- -x -y -z");

            result.HasOption("o")
                  .Should().BeTrue();

            result.AppliedOptions
                  .Should()
                  .HaveCount(1);
        }

        [Fact]
        public void The_portion_of_the_command_line_following_a_double_slash_is_accessible_as_UnparsedTokens()
        {
            var result = new Parser(
                    Option("-o", ""))
                .Parse("-o \"some stuff\" -- x y z");

            result.UnparsedTokens
                  .Should()
                  .ContainInOrder("x", "y", "z");
        }

        [Fact]
        public void When_a_required_argument_is_not_supplied_then_an_error_is_returned()
        {
            var parser = new Parser(Option("-x", "", ExactlyOneArgument));

            var result = parser.Parse("-x");

            result.Errors
                  .Should()
                  .Contain(e => e.Message == "Required argument missing for option: -x");
        }

        [Fact]
        public void When_an_option_has_en_error_then_the_error_has_a_reference_to_the_option()
        {
            var option = Option("-x", "", AnyOneOf("this", "that"));

            var parser = new Parser(option);

            var result = parser.Parse("-x something_else");

            result.Errors
                  .Where(e => e.Option != null)
                  .Should()
                  .Contain(e => e.Option.Name == option.Name);
        }

        [Fact]
        public void When_no_option_accepts_arguments_but_one_is_supplied_then_an_error_is_returned()
        {
            var parser = new Parser(Option("-x", "", NoArguments));

            var result = parser.Parse("-x some-arg");

            result.Errors
                  .Select(e => e.Message)
                  .Should()
                  .ContainSingle(e => e == "Option 'some-arg' is not recognized.");
        }

        [Fact]
        public void When_an_option_accepts_only_specific_arguments_but_a_wrong_one_is_supplied_then_an_informative_error_is_returned()
        {
            var parser = new Parser(
                Option("-x", "",
                       AnyOneOf("this", "that", "the-other-thing")));

            var result = parser.Parse("-x none-of-those");

            result.Errors
                  .Should()
                  .Contain(e => e.Message == "Required argument missing for option: -x");
        }

        [Fact]
        public void Parser_options_can_supply_context_sensitive_matches()
        {
            var parser = new Parser(
                Option("--bread", "",
                       AnyOneOf("wheat", "sourdough", "rye")),
                Option("--cheese", "",
                       AnyOneOf("provolone", "cheddar", "cream cheese")));

            var result = parser.Parse("--bread ");

            result.Suggestions()
                  .Should()
                  .BeEquivalentTo("rye", "sourdough", "wheat");

            result = parser.Parse("--bread wheat ---cheese ");

            result.Suggestions()
                  .Should()
                  .BeEquivalentTo("cheddar", "cream cheese", "provolone");
        }

        [Fact]
        public void Short_form_arguments_can_be_specified_using_equals_delimiter()
        {
            var parser = new Parser(Option("-x", "", ExactlyOneArgument));

            var result = parser.Parse("-x=some-value");

            result.Errors.Should().BeEmpty();

            result["x"].Arguments.Should().ContainSingle(a => a == "some-value");
        }

        [Fact]
        public void Long_form_arguments_can_be_specified_using_equals_delimiter()
        {
            var parser = new Parser(Option("--hello", "", ExactlyOneArgument));

            var result = parser.Parse("--hello=there");

            result.Errors.Should().BeEmpty();

            result["hello"].Arguments.Should().ContainSingle(a => a == "there");
        }

        [Fact]
        public void Short_form_arguments_can_be_specified_using_colon_delimiter()
        {
            var parser = new Parser(Option("-x", "", ExactlyOneArgument));

            var result = parser.Parse("/x:some-value");

            result.Errors.Should().BeEmpty();

            result["x"].Arguments.Should().ContainSingle(a => a == "some-value");
        }

        [Fact]
        public void Long_form_arguments_can_be_specified_using_colon_delimiter()
        {
            var parser = new Parser(Option("--hello", "", ExactlyOneArgument));

            var result = parser.Parse("/hello:there");

            result.Errors.Should().BeEmpty();

            result["hello"].Arguments.Should().ContainSingle(a => a == "there");
        }

        [Fact]
        public void Argument_short_forms_can_be_bundled()
        {
            var parser = new Parser(
                Option("-x", "", NoArguments),
                Option("-y", "", NoArguments),
                Option("-z", "", NoArguments));

            var result = parser.Parse("-xyz");

            result.AppliedOptions
                  .Select(o => o.Name)
                  .Should()
                  .BeEquivalentTo("x", "y", "z");
        }

        [Fact]
        public void Options_can_be_specified_multiple_times_and_their_arguments_are_collated()
        {
            var parser = new Parser(
                Option("-a|--animals", "", ZeroOrMoreArguments),
                Option("-v|--vegetables", "", ZeroOrMoreArguments));

            var result = parser.Parse("-a cat -v carrot -a dog");

            result["animals"].Arguments
                             .Should()
                             .BeEquivalentTo("cat", "dog");

            result["vegetables"].Arguments
                                .Should()
                                .BeEquivalentTo("carrot");
        }

        [Fact]
        public void Option_with_multiple_nested_options_allowed_is_parsed_correctly()
        {
            var option = Command("outer", "",
                                 Option("inner1", "", ExactlyOneArgument),
                                 Option("inner2", "", ExactlyOneArgument));

            var parser = new Parser(option);

            var result = parser.Parse("outer inner1 argument1 inner2 argument2");

            System.Console.WriteLine(result);

            var applied = result.AppliedOptions.Single();

            applied
                .ValidateAll()
                .Should()
                .BeEmpty();

            applied.AppliedOptions
                   .Should()
                   .ContainSingle(o =>
                                      o.Name == "inner1" &&
                                      o.Arguments.Single() == "argument1");
            applied.AppliedOptions
                   .Should()
                   .ContainSingle(o =>
                                      o.Name == "inner2" &&
                                      o.Arguments.Single() == "argument2");
        }

        [Fact]
        public void Relative_order_of_arguments_and_options_does_not_matter()
        {
            var parser = new Parser(
                Command("move", "",
                        OneOrMoreArguments,
                        Option("-x", "", ExactlyOneArgument)));

            // option before args
            var result1 = parser.Parse(
                "move -x the-option arg1 arg2");

            // option between two args
            var result2 = parser.Parse(
                "move arg1 -x the-option arg2");

            // option after args
            var result3 = parser.Parse(
                "move arg1 arg2 -x the-option");

            // arg order reversed
            var result4 = parser.Parse(
                "move arg2 arg1 -x the-option");

            // all should be equivalent
            result1.ShouldBeEquivalentTo(result2);
            result1.ShouldBeEquivalentTo(result3);
            result1.ShouldBeEquivalentTo(result4);
        }

        [Fact]
        public void An_error_is_returned_when_multiple_sibling_commands_are_passed()
        {
            var option = Command("outer", "",
                                 Command("inner1", "", ExactlyOneArgument),
                                 Command("inner2", "", ExactlyOneArgument));

            var parser = new Parser(option);

            var result = parser.Parse("outer inner1 argument1 inner2 argument2");

            result.Errors
                  .Select(e => e.Message)
                  .Should()
                  .Contain("Command 'outer' only accepts a single subcommand but 2 were provided: inner1, inner2");
        }

        [Fact]
        public void When_child_option_will_not_accept_arg_then_parent_can()
        {
            var parser = new Parser(
                Command("the-command", "",
                        ZeroOrMoreArguments,
                        Option("-x", "", NoArguments)));

            var result = parser.Parse("the-command -x two");

            var theCommand = result["the-command"];
            theCommand["x"].Arguments.Should().BeEmpty();
            theCommand.Arguments.Should().BeEquivalentTo("two");
        }

        [Fact]
        public void When_parent_option_will_not_accept_arg_then_child_can()
        {
            var parser = new Parser(
                Command("the-command", "",
                        NoArguments,
                        Option("-x", "", ExactlyOneArgument)));

            var result = parser.Parse("the-command -x two");

            var theCommand = result["the-command"];

            theCommand["x"].Arguments.Should().BeEquivalentTo("two");
            theCommand.Arguments.Should().BeEmpty();
        }

        [Fact]
        public void When_the_same_option_is_defined_on_both_outer_and_inner_command_and_specified_at_the_end_then_it_attaches_to_the_outer_command()
        {
            var parser = new Parser(Command("outer", "", NoArguments,
                                            Command("inner", "",
                                                    Option("-x", "")),
                                            Option("-x", "")));

            var result = parser.Parse("outer inner -x");

            result["outer"]["inner"]
                .AppliedOptions
                .Should()
                .BeEmpty();
            result["outer"]
                .AppliedOptions
                .Should()
                .ContainSingle(o => o.Name == "x");
        }

        [Fact]
        public void When_the_same_option_is_defined_on_both_outer_and_inner_command_and_specified_in_between_then_it_attaches_to_the_outer_command()
        {
            var parser = new Parser(Command("outer", "",
                                            Command("inner", "",
                                                    Option("-x", "")),
                                            Option("-x", "")));

            var result = parser.Parse("outer -x inner");

            result["outer"]["inner"]
                .AppliedOptions
                .Should()
                .BeEmpty();
            result["outer"]
                .AppliedOptions
                .Should()
                .ContainSingle(o => o.Name == "x");
        }

        [Fact]
        public void When_args_have_names_matching_options()
        {
            var command = Command("the-command", "",
                                  Command("complete", "",
                                          ExactlyOneArgument,
                                          Option("--position", "",
                                                 ExactlyOneArgument)));

            var result = command.Parse("the-command",
                                       "complete",
                                       "--position",
                                       "7",
                                       "the-command");

            var complete = result["the-command"]["complete"];

            complete.Arguments.Should().BeEquivalentTo("the-command");
        }

        [Fact]
        public void A_root_command_can_be_omitted_from_the_parsed_args()
        {
            var command = Command("outer",
                                  "",
                                  Command("inner", "", Option("-x", "", ExactlyOneArgument)));

            var result1 = command.Parse("inner -x hello");
            var result2 = command.Parse("outer inner -x hello");

            result1.Diagram().Should().Be(result2.Diagram());
        }

        [Fact]
        public void A_root_command_can_match_a_full_path_to_an_executable()
        {
            var command = Command("outer",
                                  "",
                                  Command("inner", "", Option("-x", "", ExactlyOneArgument)));

            var result1 = command.Parse("inner -x hello");

            var exePath = Path.Combine("dev", "outer.exe");
            var result2 = command.Parse($"{exePath} inner -x hello");

            result1.Diagram().Should().Be(result2.Diagram());
        }

        [Fact]
        public void Absolute_unix_style_paths_are_lexed_correctly()
        {
            var command =
                @"rm ""/temp/the file.txt""";

            var parser = new Parser(
                Command("rm", "", ZeroOrMoreArguments));

            var result = parser.Parse(command);

            result.AppliedOptions["rm"]
                  .Arguments
                  .Should()
                  .OnlyContain(a => a == @"/temp/the file.txt");
        }

        [Fact]
        public void Absolite_Windows_style_paths_are_lexed_correctly()
        {
            var command =
                @"rm ""c:\temp\the file.txt\""";

            var parser = new Parser(
                Command("rm", "", ZeroOrMoreArguments));

            var result = parser.Parse(command);

            Console.WriteLine(result);

            result.AppliedOptions["rm"]
                  .Arguments
                  .Should()
                  .OnlyContain(a => a == @"c:\temp\the file.txt\");
        }
    }
}