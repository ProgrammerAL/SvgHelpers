/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "../../SvgHelpers/wwwroot/index.html",
    "../../SvgHelpers/App.razor",
    "../../SvgHelpers/Pages/*.{html,razor}",
    "../../SvgHelpers/Pages/**/*.{html,razor}",
    "../../SvgHelpers/Components/*.{html,razor}",
    "../../SvgHelpers/Components/**/*.{html,razor}",
    "../../SvgHelpers/Shared/*.{html,razor}",
  ],
  theme: {
    extend: {},
  },
  plugins: [],
}
