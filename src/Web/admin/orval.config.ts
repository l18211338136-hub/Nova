import { defineConfig } from 'orval';
import { loadEnv } from 'vite';

const env = loadEnv('development', process.cwd(), '');
const targetUrl = `${env.API_BASE_URL}/openapi/v1.json`;

export default defineConfig({
  api: {
    input: {
      target: targetUrl,
      override: {
        transformer: (api) => {
          console.log("TRANSFORMER RAN");
          if (api.components && api.components.schemas) {
            const schemas = api.components.schemas;
            const renamed: Record<string, any> = {};
            const renameMap: Record<string, string> = {};
            
            Object.keys(schemas).forEach(key => {
              let newKey = key.replace(/(Command|Result)$/, '');
              if (newKey !== key) {
                renameMap[`#/components/schemas/${key}`] = `#/components/schemas/${newKey}`;
              }
              renamed[newKey] = schemas[key];
            });
            api.components.schemas = renamed;
            
            const replaceRefs = (obj: any) => {
              if (typeof obj !== 'object' || obj === null) return;
              if (obj.$ref && renameMap[obj.$ref]) {
                obj.$ref = renameMap[obj.$ref];
              }
              for (const k in obj) {
                replaceRefs(obj[k]);
              }
            };
            replaceRefs(api);
          }
          return api;
        }
      }
    },
    output: {
      mode: 'tags-split',
      target: 'src/api/endpoints',
      schemas: 'src/api/model',
      client: 'react-query',
      httpClient: 'axios',
      mock: false,
      override: {
        mutator: {
          path: 'src/lib/api-client.ts',
          name: 'customInstance',
        },
        operationName: (operation, route, verb) => {
          let name = '';
          if (operation.operationId) {
            name = operation.operationId;
          } else {
            const parts = route.split('/').filter(Boolean);
            const lastPart = parts[parts.length - 1];
            name = lastPart.replace(/-([a-z])/g, (g) => g[1].toUpperCase());
          }
          // Ensure camelCase (lowercase first letter) for functions
          return name.charAt(0).toLowerCase() + name.slice(1);
        },
      },
    },
  },
});
