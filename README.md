IMDb Poster Downloader
======================

[![Build Status](https://img.shields.io/github/actions/workflow/status/kevinoid/ImdbPosterDownloader/dotnet.yml?branch=main&style=flat&label=build)](https://github.com/kevinoid/ImdbPosterDownloader/actions/workflows/dotnet.yml?query=branch%3Amain)
[![Coverage](https://img.shields.io/codecov/c/github/kevinoid/ImdbPosterDownloader.svg?style=flat)](https://codecov.io/github/kevinoid/ImdbPosterDownloader?branch=main)

A simple tool to download movie/episode poster images from
[IMDb](https://www.imdb.com/) using [Selenium
BiDi](https://www.selenium.dev/documentation/webdriver/bidi/).


## Introductory Examples

To download all episode posters for [Bob's
Burgers](https://www.imdb.com/title/tt1561755/):

```sh
ImdbPosterDownloader https://www.imdb.com/title/tt1561755/episodes/
```

To download the poster for the show [Bob's
Burgers](https://www.imdb.com/title/tt1561755/):

```sh
ImdbPosterDownloader https://www.imdb.com/title/tt1561755/
```


## Features

* Can download posters for each episode of multi-episode shows.


## Installation

Currently this tool must be built from source.


## Contributing

Contributions are welcome.  See the [Contributing Guidelines](CONTRIBUTING.md)
and [Code of
Conduct](https://www.contributor-covenant.org/version/1/4/code-of-conduct.html).
for details.


## License

This project is available under the terms of the [MIT License](LICENSE.txt).
See the [summary at TLDRLegal](https://tldrlegal.com/license/mit-license).

The [template](https://github.com/kevinoid/ImdbPosterDownloader) upon which this
project is based is available under the terms of
[CC0 1.0 Universal](https://creativecommons.org/publicdomain/zero/1.0/).
