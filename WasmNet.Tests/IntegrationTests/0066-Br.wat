;; invoke: br
;; expect: (i32:42)

(module
    (func (export "br") (result i32) 
        block $b
            br $b
            i32.const 0
            return
        end
        i32.const 42
    )
)