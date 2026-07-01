import { defineConfig } from 'vite';
import legacy from '@vitejs/plugin-legacy';

export default defineConfig({
    root: '.',
    server: {
        port: 5173,
        proxy: {
            '/api': {
                target: 'http://localhost:9117',
                changeOrigin: true,
                secure: false
            },
            '/torznab': {
                target: 'http://localhost:9117',
                changeOrigin: true,
                secure: false
            },
            '/potato': {
                target: 'http://localhost:9117',
                changeOrigin: true,
                secure: false
            }
        }
    },
    build: {
        outDir: 'dist',
        rollupOptions: {
            input: {
                index: 'index.html',
                login: 'login.html'
            },
            output: {
                manualChunks(id) {
                    if (id.includes('node_modules') && (id.includes('datatables.net-bs5'))) {
                        return 'datatables-bs5';
                    }
                },
                entryFileNames: `assets/[name].js`,
                chunkFileNames: `assets/[name].js`,
                assetFileNames: `assets/[name].[ext]`
            },
        },
        cssTarget: 'es2015',
    },
    optimizeDeps: {
        include: [
            'jquery',
            'bootstrap',
            'bootstrap-multiselect',
            'datatables.net-bs5'
        ]
    },
    plugins: [
        legacy({
            // This has 92.0 % coverage.
            targets: ['> 0.1%', 'not dead'],
            modernPolyfills: true,
            polyfills: true,
            renderLegacyChunks: true,
            additionalLegacyPolyfills: ['regenerator-runtime/runtime'],
        }),
    ]
});
