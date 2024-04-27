/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "../../SvgMover/wwwroot/index.html",
    "../../SvgMover/App.razor",
    "../../SvgMover/Pages/*.{html,razor}",
    "../../SvgMover/Pages/**/*.{html,razor}",
    "../../SvgMover/Components/*.{html,razor}",
    "../../SvgMover/Components/**/*.{html,razor}",
    "../../SvgMover/Shared/*.{html,razor}",
  ],
  theme: {
    extend: {},
  },
  plugins: [],
}
