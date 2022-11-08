import type { AppProps } from "next/app";
import dynamic from "next/dynamic";
import { GeistProvider, CssBaseline } from "@geist-ui/core";

import "../styles/fonts.css";
import "../styles/custom.css";

import Layout from "../components/Layout";

function MyApp({ Component, pageProps }: AppProps) {
	return (
		<GeistProvider themeType="dark">
			<CssBaseline />
			<Layout>
				<Component {...pageProps} />
			</Layout>
		</GeistProvider>
	);
}

export default dynamic(() => Promise.resolve(MyApp), {
	ssr: false,
});
